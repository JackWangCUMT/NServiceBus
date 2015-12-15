namespace NServiceBus
{
    using System.Threading.Tasks;
    using System.Xml.Linq;

    interface IRoutingFileAccess
    {
        Task<XDocument> Load(string path);
    }
}