var robot = Require<Robot>();
robot.Respond("updog", msg => msg.Send("What's up dog?"));