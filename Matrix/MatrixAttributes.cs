using System;

namespace Matrix
{
    [AttributeUsage(AttributeTargets.Method)]
    public class MatrixSpec : Attribute
    {
        public EMatrixSpecApiVersion MinVersion { get; }
        public EMatrixSpecApiVersion LastVersion { get; }
        public EMatrixSpecApi Api { get; }
        public string Path { get; }
        private const string MATRIX_SPEC_URL = "http://matrix.org/docs/spec/";
        public MatrixSpec(EMatrixSpecApiVersion supportedVer, EMatrixSpecApi api, string path, EMatrixSpecApiVersion lastVersion = EMatrixSpecApiVersion.Unknown)
        {
            Api = api;
            Path = path;
            MinVersion = supportedVer;
            LastVersion = lastVersion;
        }

        public override string ToString ()
        {
            var verStr = GetStringForVersion(LastVersion);
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
            return $"{MATRIX_SPEC_URL}/{apiStr}/{verStr}.html#${Path}";
        }

        public static string GetStringForVersion(EMatrixSpecApiVersion version)
        {
            switch (version)
            {
                case EMatrixSpecApiVersion.R001:
                    return "r0.0.1";
                case EMatrixSpecApiVersion.R010:
                    return "r0.1.0";
                case EMatrixSpecApiVersion.R020:
                    return "r0.2.0";
                case EMatrixSpecApiVersion.R030:
                    return "r0.3.0";
                case EMatrixSpecApiVersion.R040:
                    return "r0.4.0";
                default:
                    return "unstable";
            }
        }
        
        public static EMatrixSpecApiVersion GetVersionForString(string version)
        {
            switch (version)
            {
                case "r0.0.1":
                    return EMatrixSpecApiVersion.R001;
                case "r0.1.0":
                    return EMatrixSpecApiVersion.R010;
                case "r0.2.0":
                    return EMatrixSpecApiVersion.R020;
                case "r0.3.0":
                    return EMatrixSpecApiVersion.R030;
                case "r0.4.0":
                    return EMatrixSpecApiVersion.R040;
                default:
                    return EMatrixSpecApiVersion.Unknown;
            }
        }
    }

    public enum EMatrixSpecApi
    {
        ClientServer,
        ApplicationService
    }
    
    public enum EMatrixSpecApiVersion
    {
        Unknown,
        Unstable,
        R001,
        R010,
        R020,
        R030,
        R040
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
