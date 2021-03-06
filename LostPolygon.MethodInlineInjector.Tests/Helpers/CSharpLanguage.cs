﻿// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.Decompiler.Ast.Transforms;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;

// Taken from ILSpy and adapted
namespace ICSharpCode.ILSpy
{
    /// <summary>
    /// Decompiler logic for C#.
    /// </summary>
    public class CSharpLanguage
    {
        bool showAllMembers = false;
        Predicate<IAstTransform> transformAbortCondition = null;

        public CSharpLanguage()
        {
        }

        public void DecompileMethod(MethodDefinition method, ITextOutput output, DecompilerSettings options)
        {
            AstBuilder codeDomBuilder = CreateAstBuilder(options, currentType: method.DeclaringType, isSingleMember: true);
            if (method.IsConstructor && !method.IsStatic && !method.DeclaringType.IsValueType) {
                // also fields and other ctors so that the field initializers can be shown as such
                AddFieldsAndCtors(codeDomBuilder, method.DeclaringType, method.IsStatic);
                RunTransformsAndGenerateCode(codeDomBuilder, output, options, new SelectCtorTransform(method));
            } else {
                codeDomBuilder.AddMethod(method);
                RunTransformsAndGenerateCode(codeDomBuilder, output, options);
            }
        }

        class SelectCtorTransform : IAstTransform
        {
            readonly MethodDefinition ctorDef;

            public SelectCtorTransform(MethodDefinition ctorDef)
            {
                this.ctorDef = ctorDef;
            }

            public void Run(AstNode compilationUnit)
            {
                ConstructorDeclaration ctorDecl = null;
                foreach (var node in compilationUnit.Children) {
                    ConstructorDeclaration ctor = node as ConstructorDeclaration;
                    if (ctor != null) {
                        if (ctor.Annotation<MethodDefinition>() == ctorDef) {
                            ctorDecl = ctor;
                        } else {
                            // remove other ctors
                            ctor.Remove();
                        }
                    }
                    // Remove any fields without initializers
                    FieldDeclaration fd = node as FieldDeclaration;
                    if (fd != null && fd.Variables.All(v => v.Initializer.IsNull))
                        fd.Remove();
                }
                if (ctorDecl.Initializer.ConstructorInitializerType == ConstructorInitializerType.This) {
                    // remove all fields
                    foreach (var node in compilationUnit.Children)
                        if (node is FieldDeclaration)
                            node.Remove();
                }
            }
        }

        public void DecompileProperty(PropertyDefinition property, ITextOutput output, DecompilerSettings options)
        {
            AstBuilder codeDomBuilder = CreateAstBuilder(options, currentType: property.DeclaringType, isSingleMember: true);
            codeDomBuilder.AddProperty(property);
            RunTransformsAndGenerateCode(codeDomBuilder, output, options);
        }

        public void DecompileField(FieldDefinition field, ITextOutput output, DecompilerSettings options)
        {
            AstBuilder codeDomBuilder = CreateAstBuilder(options, currentType: field.DeclaringType, isSingleMember: true);
            if (field.IsLiteral) {
                codeDomBuilder.AddField(field);
            } else {
                // also decompile ctors so that the field initializer can be shown
                AddFieldsAndCtors(codeDomBuilder, field.DeclaringType, field.IsStatic);
            }
            RunTransformsAndGenerateCode(codeDomBuilder, output, options, new SelectFieldTransform(field));
        }

        /// <summary>
        /// Removes all top-level members except for the specified fields.
        /// </summary>
        sealed class SelectFieldTransform : IAstTransform
        {
            readonly FieldDefinition field;

            public SelectFieldTransform(FieldDefinition field)
            {
                this.field = field;
            }

            public void Run(AstNode compilationUnit)
            {
                foreach (var child in compilationUnit.Children) {
                    if (child is EntityDeclaration) {
                        if (child.Annotation<FieldDefinition>() != field)
                            child.Remove();
                    }
                }
            }
        }

        void AddFieldsAndCtors(AstBuilder codeDomBuilder, TypeDefinition declaringType, bool isStatic)
        {
            foreach (var field in declaringType.Fields) {
                if (field.IsStatic == isStatic)
                    codeDomBuilder.AddField(field);
            }
            foreach (var ctor in declaringType.Methods) {
                if (ctor.IsConstructor && ctor.IsStatic == isStatic)
                    codeDomBuilder.AddMethod(ctor);
            }
        }

        public void DecompileEvent(EventDefinition ev, ITextOutput output, DecompilerSettings options)
        {
            AstBuilder codeDomBuilder = CreateAstBuilder(options, currentType: ev.DeclaringType, isSingleMember: true);
            codeDomBuilder.AddEvent(ev);
            RunTransformsAndGenerateCode(codeDomBuilder, output, options);
        }

        public void DecompileType(TypeDefinition type, ITextOutput output, DecompilerSettings options)
        {
            AstBuilder codeDomBuilder = CreateAstBuilder(options, currentModule: type.Module);
            codeDomBuilder.AddType(type);
            RunTransformsAndGenerateCode(codeDomBuilder, output, options);
        }

        void RunTransformsAndGenerateCode(AstBuilder astBuilder, ITextOutput output, DecompilerSettings options, IAstTransform additionalTransform = null)
        {
            astBuilder.RunTransformations(transformAbortCondition);
            if (additionalTransform != null) {
                additionalTransform.Run(astBuilder.SyntaxTree);
            }

            astBuilder.GenerateCode(output);
        }

        public static string GetPlatformDisplayName(ModuleDefinition module)
        {
            switch (module.Architecture) {
                case TargetArchitecture.I386:
                    if ((module.Attributes & ModuleAttributes.Preferred32Bit) == ModuleAttributes.Preferred32Bit)
                        return "AnyCPU (32-bit preferred)";
                    else if ((module.Attributes & ModuleAttributes.Required32Bit) == ModuleAttributes.Required32Bit)
                        return "x86";
                    else
                        return "AnyCPU (64-bit preferred)";
                case TargetArchitecture.AMD64:
                    return "x64";
                case TargetArchitecture.IA64:
                    return "Itanium";
                default:
                    return module.Architecture.ToString();
            }
        }

        public static string GetPlatformName(ModuleDefinition module)
        {
            switch (module.Architecture) {
                case TargetArchitecture.I386:
                    if ((module.Attributes & ModuleAttributes.Preferred32Bit) == ModuleAttributes.Preferred32Bit)
                        return "AnyCPU";
                    else if ((module.Attributes & ModuleAttributes.Required32Bit) == ModuleAttributes.Required32Bit)
                        return "x86";
                    else
                        return "AnyCPU";
                case TargetArchitecture.AMD64:
                    return "x64";
                case TargetArchitecture.IA64:
                    return "Itanium";
                default:
                    return module.Architecture.ToString();
            }
        }

        public static string GetRuntimeDisplayName(ModuleDefinition module)
        {
            switch (module.Runtime) {
                case TargetRuntime.Net_1_0:
                    return ".NET 1.0";
                case TargetRuntime.Net_1_1:
                    return ".NET 1.1";
                case TargetRuntime.Net_2_0:
                    return ".NET 2.0";
                case TargetRuntime.Net_4_0:
                    return ".NET 4.0";
            }
            return null;
        }

        AstBuilder CreateAstBuilder(DecompilerSettings options, ModuleDefinition currentModule = null, TypeDefinition currentType = null, bool isSingleMember = false)
        {
            if (currentModule == null)
                currentModule = currentType.Module;
            //DecompilerSettings settings = options.DecompilerSettings;
            //if (isSingleMember) {
            //    settings = settings.Clone();
            //    settings.UsingDeclarations = false;
            //}
            return new AstBuilder(
                new DecompilerContext(currentModule) {
                    CurrentType = currentType,
                    Settings = options
                });
        }

        public string TypeToString(TypeReference type, bool includeNamespace, ICustomAttributeProvider typeAttributes = null)
        {
            ConvertTypeOptions options = ConvertTypeOptions.IncludeTypeParameterDefinitions;
            if (includeNamespace)
                options |= ConvertTypeOptions.IncludeNamespace;

            return TypeToString(options, type, typeAttributes);
        }

        string TypeToString(ConvertTypeOptions options, TypeReference type, ICustomAttributeProvider typeAttributes = null)
        {
            AstType astType = AstBuilder.ConvertType(type, typeAttributes, options);

            StringWriter w = new StringWriter();
            if (type.IsByReference) {
                ParameterDefinition pd = typeAttributes as ParameterDefinition;
                if (pd != null && (!pd.IsIn && pd.IsOut))
                    w.Write("out ");
                else
                    w.Write("ref ");

                if (astType is ComposedType && ((ComposedType)astType).PointerRank > 0)
                    ((ComposedType)astType).PointerRank--;
            }

            astType.AcceptVisitor(new CSharpOutputVisitor(w, TypeToStringFormattingOptions));
            return w.ToString();
        }

        static readonly CSharpFormattingOptions TypeToStringFormattingOptions = FormattingOptionsFactory.CreateEmpty();


        public string FormatPropertyName(PropertyDefinition property, bool? isIndexer)
        {
            if (property == null)
                throw new ArgumentNullException("property");

            if (!isIndexer.HasValue) {
                isIndexer = property.IsIndexer();
            }
            if (isIndexer.Value) {
                var buffer = new System.Text.StringBuilder();
                var accessor = property.GetMethod ?? property.SetMethod;
                if (accessor.HasOverrides) {
                    var declaringType = accessor.Overrides.First().DeclaringType;
                    buffer.Append(TypeToString(declaringType, includeNamespace: true));
                    buffer.Append(@".");
                }
                buffer.Append(@"this[");
                bool addSeparator = false;
                foreach (var p in property.Parameters) {
                    if (addSeparator)
                        buffer.Append(@", ");
                    else
                        addSeparator = true;
                    buffer.Append(TypeToString(p.ParameterType, includeNamespace: true));
                }
                buffer.Append(@"]");
                return buffer.ToString();
            } else
                return property.Name;
        }

        public string FormatMethodName(MethodDefinition method)
        {
            if (method == null)
                throw new ArgumentNullException("method");

            return (method.IsConstructor) ? method.DeclaringType.Name : method.Name;
        }

        public string FormatTypeName(TypeDefinition type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            return TypeToString(ConvertTypeOptions.DoNotUsePrimitiveTypeNames | ConvertTypeOptions.IncludeTypeParameterDefinitions, type);
        }

        public bool ShowMember(MemberReference member)
        {
            return showAllMembers || !AstBuilder.MemberIsHidden(member, new DecompilerSettings());
        }
    }
}
