using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.DXGI;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ZenithEngine.DXHelper
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
    public class AssemblyElement : Attribute
    {
        public InputElement Layout { get; }
        public int Order { get; }

        public AssemblyElement(string name, Format format, [CallerLineNumber]int order = 0)
        {
            Layout = new InputElement(name, 0, format, 0);
            Order = order;
        }

        public AssemblyElement(string name, Format format, int offset, [CallerLineNumber]int order = 0)
        {
            Layout = new InputElement(name, 0, format, offset, 0);
            Order = order;
        }

        public static AssemblyElement GetAttributeFromMember(MemberInfo member)
        {
            return (AssemblyElement)member.GetCustomAttributes(typeof(AssemblyElement), false).Single();
        }

        public static MemberInfo[] GetMembersOnType(Type type)
        {
            List<MemberInfo> members = new List<MemberInfo>();
            members.AddRange(type.GetFields());
            members.AddRange(type.GetProperties());
            var parts = members.Where(p => IsDefined(p, typeof(AssemblyElement)))
                .OrderBy(p => GetAttributeFromMember(p).Order)
                .ToArray();
            return members.ToArray();
        }
    }
}
