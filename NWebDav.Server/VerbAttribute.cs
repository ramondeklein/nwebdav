using System;
using System.Linq;

namespace NWebDav.Server
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class VerbAttribute : Attribute
    {
        private string _verb;

        public VerbAttribute(string verb)
        {
            Verb = verb;
        }

        public string Verb
        {
            get { return _verb; }
            set
            {
                // A verb must be specified
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                // A verb should be capital only
                if (!value.All(char.IsUpper))
                    throw new ArgumentException("The verb should be all upper-case letters", nameof(value));

                // Save verb
                _verb = value;
            }
        }
    }
}
