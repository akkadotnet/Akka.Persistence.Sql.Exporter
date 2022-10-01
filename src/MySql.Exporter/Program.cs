﻿// -----------------------------------------------------------------------
//  <copyright file="Program.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

await using var docker = new MySqlDocker();
docker.OnStdOut += (_, outputArgs) =>
{
    Console.WriteLine(outputArgs.Output);
};
await docker.StartAsync();
        
var config = ConfigurationFactory.ParseString($@"
                akka.loglevel = DEBUG
                akka.persistence.journal {{
                    plugin = ""akka.persistence.journal.mysql""
                    mysql {{
                        auto-initialize = on
                        connection-string = ""{docker.ConnectionString}""
                    }}
                }}
                akka.persistence.snapshot-store {{
                    plugin = ""akka.persistence.snapshot-store.mysql""
                    mysql {{
                        auto-initialize = on
                        connection-string = ""{docker.ConnectionString}""
                    }}
                }}").WithFallback(MySqlPersistence.DefaultConfiguration());
            
using var sys = ActorSystem.Create("actorSystem", config);

await sys.CreateTestData();

//Console.WriteLine(">>>>>>>>>>> Creating backup");
//await docker.DumpDatabase("backup.sql");
            
Console.WriteLine(">>>>>>>>>>> downloading backup");
//await docker.DownloadAsync("backup.sql", docker.OutputPath, "backup.tar");
            
await docker.DownloadAsync("/var/lib/mysql/", docker.OutputPath, "mysql.tar");
            
Console.WriteLine(">>>>>>>>>>> DONE!");