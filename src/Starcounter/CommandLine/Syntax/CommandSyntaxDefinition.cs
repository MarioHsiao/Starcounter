
namespace Starcounter.CommandLine.Syntax
{
    public sealed class CommandSyntaxDefinition : SyntaxDefinition, ICommandSyntax
    {
        public readonly string Name;
        public readonly string Description;
        public int? MinParameterCount { get; set; }
        public int? MaxParameterCount { get; set; }

        internal CommandSyntaxDefinition(string name, string description)
            : base()
        {
            this.Name = name;
            this.Description = description;
            this.MinParameterCount = null;
            this.MaxParameterCount = null;
        }

        #region ICommandSyntax Members

        string ICommandSyntax.Name
        {
            get { return this.Name; }
        }

        string ICommandSyntax.CommandDescription
        {
            get { return this.Description; }
        }

        int? ICommandSyntax.MinParameterCount
        {
            get { return this.MinParameterCount; }
        }

        int? ICommandSyntax.MaxParameterCount
        {
            get { return this.MaxParameterCount; }
        }

        OptionInfo[] ICommandSyntax.Properties
        {
            get
            {
                return this.CreateOptionSet(this.Properties);
            }
        }

        OptionInfo[] ICommandSyntax.Flags
        {
            get
            {
                return this.CreateOptionSet(this.Flags);
            }
        }

        #endregion
    }
}
