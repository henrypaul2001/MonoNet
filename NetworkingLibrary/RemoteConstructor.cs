using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkingLibrary
{
    [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false)]
    public class RemoteConstructor : Attribute
    {
    }
}
