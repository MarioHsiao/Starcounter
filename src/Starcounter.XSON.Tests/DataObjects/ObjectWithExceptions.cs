namespace Starcounter.Internal.XSON.Tests {
    public class ObjectWithExceptions {
        /// <summary>
        /// 
        /// </summary>
        public string Name {
            get {
                // Provoke NullReferenceException
                Person nullPerson = null;
                return nullPerson.FirstName;
            }
            set {
                throw ErrorCode.ToException(Error.SCERRINVALIDOBJECTACCESS);
            }
        }
    }
}
