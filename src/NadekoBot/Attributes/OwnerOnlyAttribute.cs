﻿using System.Threading.Tasks;
using Discord.Commands;
using Discord;
using System;

namespace NadekoBot.Attributes
{
    public class OwnerOnlyAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissions(CommandContext context, CommandInfo executingCommand,IDependencyMap depMap) =>
            Task.FromResult((NadekoBot.Credentials.IsOwner(context.User) ? PreconditionResult.FromSuccess() : PreconditionResult.FromError("Not owner")));
    }
}