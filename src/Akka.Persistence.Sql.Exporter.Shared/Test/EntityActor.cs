﻿// -----------------------------------------------------------------------
//  <copyright file="PersistenceActor.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using Akka.Actor;
using Akka.Event;
using Akka.Persistence.Journal;

namespace Akka.Persistence.Sql.Exporter.Shared.Test;

public sealed class EntityActor : ReceivePersistentActor
{
    public static Props Props(string id) => Actor.Props.Create(() => new EntityActor(id));

    private readonly ILoggingAdapter _log;
    private int _total;
    private int _persisted;
    private readonly string[] _tags = Const.Tags;

    public EntityActor(string persistenceId)
    {
        _log = Context.GetLogger();
        
        PersistenceId = persistenceId;
        
        Command<int>(msg => Persist(msg, i =>
        {
            _total += i;
            _persisted++;
        }));

        Command<string>(msg => Persist(msg, str =>
        {
            _total += int.Parse(str);
            _persisted++;
        }));

        Command<ShardedMessage>(msg =>
        {
            object obj = (msg.Message % 4) switch
            {
                0 => msg,
                1 => new Tagged(msg, new[] { _tags[0] }),
                2 => new Tagged(msg, new[] { _tags[0], _tags[1] }),
                _ => new Tagged(msg, new[] { _tags[0], _tags[1], _tags[2] })
            };
            
            if(obj is Tagged tagged)
            {
                Persist(tagged, sm =>
                {
                    _total += ((ShardedMessage)sm.Payload).Message;
                    _persisted++;
                });
            }
            else
            {
                Persist(msg, sm =>
                {
                    _total += sm.Message;
                    _persisted++;
                });
            }
        });

        Command<Finish>(_ =>
        {
            Sender.Tell((PersistenceId, _persisted));
        });

        Recover<int>(msg =>
        {
            _total += msg;
            _persisted++;
        });
        
        Recover<string>(msg =>
        {
            _total += int.Parse(msg);
            _persisted++;
        });
        
        Recover<ShardedMessage>(msg =>
        {
            _total += msg.Message;
            _persisted++;
        });
    }

    public override string PersistenceId { get; }

    protected override void PreStart()
    {
        _log.Debug($"EntityActor({PersistenceId}) started");
        base.PreStart();
    }
}