using System;

namespace Matrix
{
    [AttributeUsage(AttributeTargets.Method)]
    public class MatrixSpec : Attribute
    {
        public EMatrixSpecApiVersion Version { get; }
        public EMatrixSpecApi Api { get; }
        public string Path { get; }
        private const string MATRIX_SPEC_URL = "http://matrix.org/docs/spec/";
        public MatrixSpec(EMatrixSpecApiVersion supportedVer, EMatrixSpecApi api, string path)
        {
            Api = api;
            Path = path;
            Version = supportedVer;
        }

        public override string ToString ()
        {
            var verStr = "";
            var apiStr = "";
            switch (Api)
            {
                case EMatrixSpecApi.ClientServer:
                    apiStr = "client_server";
                    break;
                case EMatrixSpecApi.ApplicationService:
                    apiStr = "application_service";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(Api), Api, null);
            }
            
            switch (Version)
            {
                case EMatrixSpecApiVersion.R001:
                    verStr = "r0.0.1";
                    break;
                case EMatrixSpecApiVersion.R010:
                    verStr = "r0.1.0";
                    break;
                case EMatrixSpecApiVersion.R020:
                    verStr = "r0.2.0";
                    break;
                case EMatrixSpecApiVersion.R030:
                    verStr = "r0.3.0";
                    break;
                case EMatrixSpecApiVersion.R040:
                    verStr = "r0.4.0";
                    break;
                // case EMatrixSpecApiVersion.Unstable:
                default:
                    verStr = "unstable";
                    break;
            }
            return $"{MATRIX_SPEC_URL}/{apiStr}/{verStr}.html#${Path}";
        }
    }

    public enum EMatrixSpecApi
    {
        ClientServer,
        ApplicationService,
    }
    
    public enum EMatrixSpecApiVersion
    {
        Unstable,
        R001,
        R010,
        R020,
        R030,
        R040,
    }

    /// <summary>
    /// The versions this method
    /// </summary>
    [AttributeUsage(
    AttributeTargets.Method |
    AttributeTargets.Property |
    AttributeTargets.Class |
    AttributeTargets.Struct |
    AttributeTargets.Field
    )]
    public class MatrixSpecVersionAttribute : Attribute {
        public readonly string[] Versions;
        public MatrixSpecVersionAttribute(params string[] versions) {
            Versions = versions;
        }
    }
}
