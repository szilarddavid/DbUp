using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DbUp.Engine;
using DbUp.Engine.Transactions;

namespace DbUp.ScriptProviders
{
    /// <summary>
    /// An enhanced <see cref="IScriptProvider"/> implementation which retrieves upgrade scripts or IScript code upgrade scripts embedded in an assembly.
    /// </summary>
    public class EmbeddedScriptAndCodeProvider : IScriptProvider
    {
        private readonly EmbeddedScriptProvider embeddedScriptProvider;
        private readonly Assembly assembly;
        private readonly Func<string, bool> filter;

        /// <summary>
        /// Script options
        /// </summary>
        public ScriptOptions ScriptOptions { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EmbeddedScriptProvider"/> class.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <param name="filter">The embedded sql script filter.</param>
        /// <param name="scriptOptions">Script options</param>
        public EmbeddedScriptAndCodeProvider(Assembly assembly, Func<string, bool> filter, ScriptOptions scriptOptions)
        {
            this.assembly = assembly;
            this.filter = filter;
            embeddedScriptProvider = new EmbeddedScriptProvider(assembly, filter);

            ScriptOptions = scriptOptions;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EmbeddedScriptProvider"/> class.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <param name="filter">The embedded script filter.</param>
        public EmbeddedScriptAndCodeProvider(Assembly assembly, Func<string, bool> filter) : this(assembly, filter, new ScriptOptions())
        {
        }

        private IEnumerable<SqlScript> ScriptsFromScriptClasses(IConnectionManager connectionManager)
        {
            var script = typeof(IScript);
            return connectionManager.ExecuteCommandsWithManagedConnection(dbCommandFactory => assembly
                .GetTypes()
                .Where(type =>
                {
                    return script.IsAssignableFrom(type) &&
#if USE_TYPE_INFO
                        type.GetTypeInfo().IsClass;
#else
                        type.IsClass;
#endif
                })
                .Select(s => (SqlScript) new LazySqlScript(s.FullName + ".cs", () => ((IScript) Activator.CreateInstance(s)).ProvideScript(dbCommandFactory)))
                .ToList());
        }

        /// <summary>
        /// Gets all scripts that should be executed.
        /// </summary>
        public IEnumerable<SqlScript> GetScripts(IConnectionManager connectionManager)
        {
            var sqlScripts = embeddedScriptProvider
                .GetScripts(connectionManager)
                .Concat(ScriptsFromScriptClasses(connectionManager))
                .OrderBy(x => x.Name)
                .Where(x => filter(x.Name))
                .ToList();

            return sqlScripts;
        }
    }
}