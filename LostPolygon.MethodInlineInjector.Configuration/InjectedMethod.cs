using System;
using LostPolygon.MethodInlineInjector.Serialization;

namespace LostPolygon.MethodInlineInjector {
    public class InjectedMethod : SimpleXmlSerializable {
        public string AssemblyPath { get; private set; }
        public string MethodFullName { get; private set; }
        public MethodInjectionPosition InjectionPosition { get; private set; } = MethodInjectionPosition.InjecteeMethodStart;

        private InjectedMethod() {
        }

        public InjectedMethod(
            string assemblyPath,
            string methodFullName,
            MethodInjectionPosition injectionPosition = MethodInjectionPosition.InjecteeMethodStart
        ) {
            AssemblyPath = assemblyPath ?? throw new ArgumentNullException(nameof(assemblyPath));
            MethodFullName = methodFullName ?? throw new ArgumentNullException(nameof(methodFullName));
            InjectionPosition = injectionPosition;
        }

        public override string ToString() {
            return $"{nameof(AssemblyPath)}: '{AssemblyPath}', " +
                   $"{nameof(MethodFullName)}: '{MethodFullName}', " +
                   $"{nameof(InjectionPosition)}: {InjectionPosition}";
        }

        #region With.Fody

        public InjectionConfiguration WithAssemblyPath(string value) => null;
        public InjectionConfiguration WithMethodFullName(string value) => null;
        public InjectionConfiguration WithInjectionPosition(MethodInjectionPosition value) => null;

        #endregion

        #region Serialization

        protected override void Serialize() {
            base.Serialize();

            SerializationHelper.ProcessStartElement(nameof(InjectedMethod));
            {
                SerializationHelper.ProcessAttributeString(nameof(AssemblyPath), s => AssemblyPath = s, () => AssemblyPath);
                SerializationHelper.ProcessAttributeString(nameof(MethodFullName), s => MethodFullName = s, () => MethodFullName);
                SerializationHelper.ProcessEnumAttribute(nameof(InjectionPosition), s => InjectionPosition = s, () => InjectionPosition);
            }
            SerializationHelper.ProcessAdvanceOnRead();
            SerializationHelper.ProcessEndElement();
        }

        #endregion
    }
}