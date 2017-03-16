﻿using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace LostPolygon.MethodInlineInjector {
    internal static class CecilExtensions {
        public static Instruction ReplaceAndFixReferences(this ILProcessor processor, Instruction oldInstruction, Instruction newInstruction) {
            processor.Replace(oldInstruction, newInstruction);
            foreach (Instruction bodyInstruction in processor.Body.Instructions) {
                ReplaceOperandInstruction(bodyInstruction, oldInstruction, newInstruction);
            }

            foreach (ExceptionHandler exceptionHandler in processor.Body.ExceptionHandlers) {
                ReplaceOperandInstruction(exceptionHandler.FilterStart, oldInstruction, newInstruction);
                ReplaceOperandInstruction(exceptionHandler.HandlerStart, oldInstruction, newInstruction);
                ReplaceOperandInstruction(exceptionHandler.HandlerEnd, oldInstruction, newInstruction);
                ReplaceOperandInstruction(exceptionHandler.TryStart, oldInstruction, newInstruction);
                ReplaceOperandInstruction(exceptionHandler.TryEnd, oldInstruction, newInstruction);
            }

            return newInstruction;
        }

        public static void CopyTo(this SequencePoint sequence, Instruction il) {
            if (sequence == null)
                return;

            Document document =
                new Document(sequence.Document.Url) {
                    Type = sequence.Document.Type,
                    Language = sequence.Document.Language,
                    LanguageVendor = sequence.Document.LanguageVendor
                };

            il.SequencePoint =
                new SequencePoint(document) {
                    EndColumn = sequence.EndColumn,
                    EndLine = sequence.EndLine,
                    StartColumn = sequence.StartColumn,
                    StartLine = sequence.StartLine
                };
        }

        public static TypeDefinition GetDefinition(this TypeReference type) {
            if (type.IsDefinition)
                return type as TypeDefinition;

            if (type.IsGenericInstance)
                return ((GenericInstanceType) type).Resolve();

            return type.Resolve();
        }

        public static string GetFullName(this MethodDefinition methodDefinition) {
            return $"{methodDefinition.DeclaringType.FullName}.{methodDefinition.Name}";
        }

        public static void AddRange<TResult>(this Collection<TResult> collection, IEnumerable<TResult> source) {
            foreach (TResult result in source)
                collection.Add(result);
        }

        public static void InsertRangeToStart<TResult>(this Collection<TResult> collection, IEnumerable<TResult> source) {
            int inserted = 0;
            foreach (TResult result in source) {
                collection.Insert(inserted, result);
                inserted++;
            }
        }

        public static TSource Last<TSource>(this Collection<TSource> source, Func<TSource, bool> predicate) {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            for (int i = source.Count - 1; i >= 0; i--) {
                TSource item = source[i];
                if (predicate(item))
                    return item;
            }

            throw new InvalidOperationException("No matching element found");
        }

        public static TSource Last<TSource>(this Collection<TSource> source) {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (source.Count == 0)
                throw new InvalidOperationException("Empty collection");

            return source[source.Count - 1];
        }

        private static void ReplaceOperandInstruction(Instruction bodyInstruction, Instruction oldInstruction, Instruction newInstruction) {
            if (bodyInstruction == null)
                return;

            if (bodyInstruction.Operand == oldInstruction) {
                bodyInstruction.Operand = newInstruction;
            }
        }
    }
}
