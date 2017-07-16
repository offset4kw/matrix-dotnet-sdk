using System;
using NUnit.Framework;
using Matrix.Client;
using Moq;
namespace Matrix.Tests
{
    [TestFixture]
    public class MatrixMediaFileTests
    {
        [Test]
        public void TestCreateMediaFile()
        {
            const string MXC_URL = "mxc://half-shot.uk/oSnvUaEqIQcsVfAuulWeeBVB";
            const string CONTENT_TYPE = "image/png";
            var mock = new Mock<MatrixAPI>();
            mock.Setup(f => f.BaseURL).Returns("https://half-shot.uk");
            MatrixMediaFile media = new MatrixMediaFile((MatrixAPI)mock.Object, MXC_URL, CONTENT_TYPE);
            Assert.That(media.GetUrl(), Is.EqualTo("https://half-shot.uk/_matrix/media/r0/download/half-shot.uk/oSnvUaEqIQcsVfAuulWeeBVB"));
            Assert.That(media.GetThumbnailUrl(256,256,"crop"), Is.EqualTo("https://half-shot.uk/_matrix/media/r0/thumbnail/half-shot.uk/oSnvUaEqIQcsVfAuulWeeBVB?width=256&height=256&method=crop"));
        }
    }
}