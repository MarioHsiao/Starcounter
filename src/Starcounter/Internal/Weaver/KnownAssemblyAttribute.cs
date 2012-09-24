using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Starcounter.Internal.Weaver {
    /// <summary>
    /// Custom attribute that, when applied on an assembly, means that this assembly
    /// is <i>known</i>, that is, <i>trusted</i>. Known assemblies may use
    /// constructions that are forbidden to normal user code. Known assemblies
    /// may required to be processed specially by the weaver, by means of
    /// <see cref="WeaverDirectives"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class KnownAssemblyAttribute : Attribute {
        private readonly WeaverDirectives weaverDirectives;
        private readonly string proof;

        private static readonly RSACryptoServiceProvider rsaDecoder;

        static KnownAssemblyAttribute() {
            // Initialize the crypto object that will verify the proofs.
            // Read the public key stored as a managed resource.
            byte[] publicKey;
            using (
                Stream stream =
                    typeof(KnownAssemblyAttribute).Assembly.GetManifestResourceStream(
                        "Sc.Server.Internal.KnownAssemblyPublicKey.csp")) {
                publicKey = new byte[stream.Length];
                stream.Read(publicKey, 0, (int)stream.Length);
            }
            // Build the crypto object.
            CspParameters cspp = new CspParameters(1);
            cspp.KeyNumber = (int)KeyNumber.Signature;
            rsaDecoder = new RSACryptoServiceProvider(cspp);
            rsaDecoder.ImportCspBlob(publicKey);
        }

        /// <summary>
        /// Initializes a new <see cref="KnownAssemblyAttribute"/>.
        /// </summary>
        /// <param name="directives">Weaver directives.</param>
        /// <param name="proof">Proof that the assembly to which the current custom attribute
        /// is trusted (a signature generated with the <see cref="MakeProof"/> method.</param>
        public KnownAssemblyAttribute(WeaverDirectives directives, string proof) {
            this.proof = proof;
            this.weaverDirectives = directives;
        }

        /// <summary>
        /// Gets the weaver directives for the assembly on which the current custom attribute
        /// was applied.
        /// </summary>
        public WeaverDirectives WeaverDirectives {
            get {
                return weaverDirectives;
            }
        }

        /// <summary>
        /// Gets the proof that the assembly on which the current custom attribute
        /// is applied, is really trusted.
        /// </summary>
        public string Proof {
            get {
                return proof;
            }
        }


        /// <summary>
        /// Makes a proof that an assembly is trusted.
        /// </summary>
        /// <param name="assemblyName">Full name of the trusted assembly.</param>
        /// <param name="privateKeyLocation">Full path of the private key.</param>
        /// <returns>A string that can be used as the proof when constructing the
        /// custom attribute.</returns>
        public static string MakeProof(string assemblyName, string privateKeyLocation) {
            CspParameters cspp = new CspParameters(1);
            cspp.KeyNumber = (int)KeyNumber.Signature;
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(cspp);
            rsa.ImportCspBlob(File.ReadAllBytes(privateKeyLocation));
            byte[] signature = rsa.SignData(Encoding.ASCII.GetBytes(assemblyName), new SHA1CryptoServiceProvider());
            return Convert.ToBase64String(signature);
        }


        /// <summary>
        /// Checks the proof that the current custom attribute can be applied on a given assembly.
        /// </summary>
        /// <param name="assemblyName">Full name of the assembly to which the current custom
        /// attribute is applied.</param>
        /// <returns><b>true</b> if the proof is valid, otherwise <b>false</b>.</returns>
        public bool CheckProof(string assemblyName) {
            byte[] signature = Convert.FromBase64String(this.proof);
            return
                rsaDecoder.VerifyData(Encoding.ASCII.GetBytes(assemblyName), new SHA1CryptoServiceProvider(),
                                      signature);
        }
    }
}