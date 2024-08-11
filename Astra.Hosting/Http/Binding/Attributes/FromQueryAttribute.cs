using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Astra.Hosting.Http.Binding.Attributes
{
    public sealed class FromQueryAttribute : Attribute
    {
        public FromQueryAttribute(string name = "")
        {
            Name = name;
        }

        public string Name { get; private set; } = "";
    }
}
