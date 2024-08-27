﻿using SqlKata.Compilers;
using SqlKata;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Collections;

namespace FStorm
{
    public abstract class Command
    {

        public readonly string CommandId;

        internal Connection? connection;
        internal Transaction? transaction;

        protected readonly IServiceProvider serviceProvider;
        protected readonly FStormService fStormService;

        public Command(IServiceProvider serviceProvider, FStormService fStormService) 
        {
            CommandId = Guid.NewGuid().ToString();
            this.serviceProvider = serviceProvider;
            this.fStormService = fStormService;
        }

        public abstract SQLCompiledQuery ToSQL();

        protected virtual SQLCompiledQuery Compile(CompilerContext<GetConfiguration> context) 
        {
            Compiler compiler = fStormService.options.SQLCompilerType switch
            {
                SQLCompilerType.MSSQL => new SqlServerCompiler(),
                SQLCompilerType.SQLLite => new SqliteCompiler(),
                _ => throw new ArgumentException("Unexpected compiler type value")
            };

            var _compilerOutput = compiler.Compile(context.Query);
            return new SQLCompiledQuery(context, _compilerOutput.Sql, _compilerOutput.NamedBindings);
        }
    }

    public class SQLCompiledQuery
    {
        public CompilerContext<GetConfiguration> Context { get; }
        public string Statement { get; }
        public Dictionary<string, object> Bindings { get; }
        public SQLCompiledQuery(CompilerContext<GetConfiguration> context, string statement, Dictionary<string, object> bindings)
        {
            Context = context;
            Statement = statement;
            Bindings = bindings;
        }
    }



}