using MMBot.Scripts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMBot.Pokemon
{
    public class Pkmn : IMMBotScript
    {
        private string _storageKey = "PKMN_{0}_{1}";
        private string _pokedexKey = "POKEDEX";
        //Check this depending on what series of pokemon you want (152 = old school)
        private int _maxPokeId = 152;
        private string _pokeBaseApi = "http://pokeapi.co/api/v1/";
        private string _mediaFormatUrl = "http://pokeapi.co/media/img/{0}.png";

        //http://www.polygon.com/2014/6/27/5850720/pokemon-battle-slack-vox

        public void Register(Robot robot)
        {
            robot.Respond("pkmn battle", async msg =>
            {
                //check if the user is already in a battle in this context
                var currentBattle = await GetCurrentBattle(msg, robot.Brain);
                if (currentBattle != null)
                {
                    await msg.Send("YOU ARE ALREADY IN A BATTLE!");
                    return;
                }
                //create a new battle for this context
                currentBattle = new PkmnBattle
                {
                    User = msg.Message.User.Id,
                    Room = msg.Message.User.Room,
                    StartTime = DateTime.Now
                };
                await msg.SendFormat(msg.Random(_msgBattleBegins), msg.Message.User.Name);

                //Pick a pokemon
                var myPkmn = await GetRandomPkmn(msg);
                if (myPkmn == null)
                {
                    await msg.Send("I ran away...");
                    return;
                }

                await msg.SendFormat(msg.Random(_msgPokeSelect), myPkmn.Name);
                await msg.SendFormat(_mediaFormatUrl, myPkmn.National_Id);

                currentBattle.BotPokemon = myPkmn;

                await SaveBattle(currentBattle, robot.Brain);
            });

            robot.Respond("pkmn I choose (.*)", async msg =>
            {
                var pkmnName = msg.Match[1];
                //get the current battle
                var currentBattle = await GetCurrentBattle(msg, robot.Brain);
                if (currentBattle == null)
                {
                    await msg.SendFormat("{0} rolls over for 0 damage...", pkmnName);
                    return;
                }
                //try find the pokemon
                var dex = await GetPokeDex(msg, robot.Brain);
                var dexmon = dex.Pokemon.FirstOrDefault(p => p.Name.Equals(pkmnName, StringComparison.OrdinalIgnoreCase));
                if (dexmon == null)
                {
                    await msg.Send("That type of Pokemon doesn't exist.");
                    return;
                }
                var myPkmn = await GetPokemon(msg, dexmon.PokeId());
                await msg.SendFormat("You chose {0}. {1}HP {2}", myPkmn.Name, myPkmn.HP, FormatMoves(myPkmn.Moves));
                await msg.SendFormat(_mediaFormatUrl, myPkmn.National_Id);
                //BATTLE!
                currentBattle.PlayerPokemon = myPkmn;
                await SaveBattle(currentBattle, robot.Brain);
            });

            robot.Respond("pkmn use (.*)", async msg =>
            {
                var moveName = msg.Match[1];
                //get the current battle
                var currentBattle = await GetCurrentBattle(msg, robot.Brain);
                if (currentBattle == null)
                {
                    await msg.Send("You set yourself on fire...");
                    return;
                }
                if (currentBattle.PlayerPokemon == null)
                {
                    await msg.Send("You need a Pokemon to join the battle!");
                    return;
                }
                var selectedMove = currentBattle.PlayerPokemon.Moves.FirstOrDefault(m => m.Name.Equals(moveName, StringComparison.OrdinalIgnoreCase));
                if (selectedMove == null)
                {
                    await msg.SendFormat("Your Pokemon doesn't know {0}", moveName);
                    return;
                }
                var move = await GetMove(msg, selectedMove.MoveId());
                
                //TODO - calcuate actual damage based off pokemon stats
                currentBattle.BotPokemon.HP -= move.Power;

                if (currentBattle.BotPokemon.HP > 0)
                {
                    //TODO - Save move to the history
                    await msg.SendFormat("You used {0} Power:{1} PP:{2}", move.Name, move.Power, move.PP);
                    await SaveBattle(currentBattle, robot.Brain);
                }
                else
                {
                    //dead
                    await msg.SendFormat("{0} fainted.", currentBattle.BotPokemon.Name);
                    //TODO - save the result to a separate list for stats
                    await ClearBattle(currentBattle, robot.Brain);
                }
            });
        }

        private string FormatMoves(List<PkmnMove> moves)
        {
            var moveString = string.Empty;
            foreach (var m in moves.Take(4))
            {
                if (!string.IsNullOrEmpty(moveString))
                {
                    moveString += ", ";
                }
                moveString += m.Name;
            }
            return moveString;
        }

        public IEnumerable<string> GetHelp()
        {
            return new List<string>();
        }


        private async Task<PkmnBattle> GetCurrentBattle(IResponse<TextMessage> msg, Brains.IBrain brain)
        {
            //check the brain yo
            var battle = await GetBattle(brain, msg.Message.User.Id, msg.Message.User.Room);

            return battle;
        }

        private Task<PkmnBattle> GetBattle(Brains.IBrain brain, string userId, string room)
        {
            return brain.Get<PkmnBattle>(string.Format(_storageKey, userId, room));
        }

        private Task SaveBattle(PkmnBattle battle, Brains.IBrain brain)
        {
            return brain.Set(string.Format(_storageKey, battle.User, battle.Room), battle);
        }

        private Task ClearBattle(PkmnBattle battle, Brains.IBrain brain)
        {
            return brain.Set<PkmnBattle>(string.Format(_storageKey, battle.User, battle.Room), null);
        }

        private async Task<PkmnPokemon> GetRandomPkmn(IResponse<TextMessage> msg)
        {
            var rand = new Random();
            var pokeId = rand.Next(_maxPokeId);

            var pokeResponse = await GetPokemon(msg, pokeId);

            return pokeResponse;
        }

        private Task<PkmnPokemon> GetPokemon(IResponse<TextMessage> msg, int pokeId)
        {
            return msg.Http(string.Format("{0}pokemon/{1}", _pokeBaseApi, pokeId)).GetJson<PkmnPokemon>();
        }

        private Task<PkmnMoveFull> GetMove(IResponse<TextMessage> msg, int moveId)
        {
            return msg.Http(string.Format("{0}move/{1}", _pokeBaseApi, moveId)).GetJson<PkmnMoveFull>();
        }

        private async Task<PkmnPokedex> GetPokeDex(IResponse<TextMessage> msg, Brains.IBrain brain)
        {
            var brainDex = await brain.Get<PkmnPokedex>(_pokedexKey);

            //TODO - should this refresh?
            if (brainDex != null)
            {
                return brainDex;
            }

            var newDex = await msg.Http(string.Format("{0}pokedex/1/", _pokeBaseApi)).GetJson<PkmnPokedex>();
            await brain.Set(_pokedexKey, newDex);

            return newDex;
        }


        private List<string> _msgBattleBegins = new List<string>
        {
            "OK {0}, the battle has begun!",
            "It's on {0}!"
        };
        private List<string> _msgPokeSelect = new List<string>
        {
            "GO {0}!",
            "I choose you {0}!"
        };
    }



    public class PkmnBattle
    {
        public string User { get; set; }
        public string Room { get; set; }
        public DateTime StartTime { get; set; }
        public PkmnPokemon BotPokemon { get; set; }
        public PkmnPokemon PlayerPokemon { get; set; }
    }

    public class PkmnPokemon
    {
        public string Name { get; set; }
        public int National_Id { get; set; }
        public int HP { get; set; }
        public List<PkmnMove> Moves { get; set; }
    }

    public class PkmnMove
    {
        public string Name { get; set; }
        public string Resource_Uri { get; set; }

        internal int MoveId()
        {
            try
            {
                var res = Resource_Uri;
                if (res.EndsWith("/"))
                {
                    res = res.Substring(0, res.Length - 1);
                }
                return Convert.ToInt32(res.Substring(res.LastIndexOf("/") + 1));
            }
            catch
            {
                return 1;
            }
        }
    }

    public class PkmnMoveFull
    {
        public string Name { get; set; }
        public int Id { get; set; }
        public string Resource_Uri { get; set; }
        public int Power { get; set; }
        public int PP { get; set; }
    }

    public class PkmnPokedex
    {
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
        public string Name { get; set; }
        public List<PkmnDexmon> Pokemon { get; set; }
    }
    public class PkmnDexmon
    {
        public string Name { get; set; }
        public string Resource_Uri { get; set; }

        internal int PokeId()
        {
            try
            {
                var res = Resource_Uri;
                if (res.EndsWith("/"))
                {
                    res = res.Substring(0, res.Length - 1);
                }
                return Convert.ToInt32(res.Substring(res.LastIndexOf("/") + 1));
            }
            catch
            {
                return 1;
            }
        }
    }
}
