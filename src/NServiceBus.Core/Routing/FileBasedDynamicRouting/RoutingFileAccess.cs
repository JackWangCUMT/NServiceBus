namespace NServiceBus.Routing
{
    using System.IO;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Linq;

    class RoutingFileAccess : IRoutingFileAccess
    {
        public Task<XDocument> Load(string path)
        {
            XDocument doc;
            using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var reader = XmlReader.Create(new StreamReader(file)))
                {
                    doc = XDocument.Load(reader);
                }
            }
            return Task.FromResult(doc);
        }
    }
}