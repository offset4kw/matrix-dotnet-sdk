using System.Collections.Generic;
using System.Linq;

namespace Matrix.Structures
{
    public class MatrixVersions
    {
        public List<string> versions;
        public Dictionary<string, bool> unstable_features;

        public List<EMatrixSpecApiVersion> supportedVersions()
        {
            return versions.ConvertAll(MatrixSpec.GetVersionForString);
        }
    }
}