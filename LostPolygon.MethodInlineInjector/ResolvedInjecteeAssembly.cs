using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace LostPolygon.MethodInlineInjector {
    public class ResolvedInjecteeAssembly {
        public AssemblyDefinition AssemblyDefinition { get; }
        public IReadOnlyList<MethodDefinition> InjecteeMethods { get; }
        public IReadOnlyList<ResolvedAllowedAssemblyReference> AllowedAssemblyReferences { get; }

        public ResolvedInjecteeAssembly(
            AssemblyDefinition assemblyDefinition,
            IReadOnlyList<MethodDefinition> injecteeMethods,
            IReadOnlyList<ResolvedAllowedAssemblyReference> allowedAssemblyReferences = null
        ) {
            AssemblyDefinition = assemblyDefinition ?? throw new ArgumentNullException(nameof(assemblyDefinition));
            InjecteeMethods = injecteeMethods ?? throw new ArgumentNullException(nameof(allowedAssemblyReferences));
            AllowedAssemblyReferences = allowedAssemblyReferences ?? Array.Empty<ResolvedAllowedAssemblyReference>();
        }

        #region With.Fody

        public ResolvedInjecteeAssembly WithInjectedMethods(IReadOnlyList<MethodDefinition> value) => null;
        public ResolvedInjecteeAssembly WithAllowedAssemblyReferences(IReadOnlyList<ResolvedAllowedAssemblyReference> value) => null;

        #endregion
    }
}