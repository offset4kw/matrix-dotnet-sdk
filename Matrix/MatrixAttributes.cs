using System;

namespace Matrix
{
    [AttributeUsage(AttributeTargets.Method)]
    public class MatrixSpec : Attribute
    {
        const string MATRIX_SPEC_URL = "http://matrix.org/docs/spec/";
        public readonly string URL;
        public MatrixSpec(string url){
            URL = MATRIX_SPEC_URL + url;
        }

        public override string ToString ()
        {
            return string.Format (URL);
        }
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
