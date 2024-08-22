using System.Xml;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Csdl;
using Microsoft.OData.Edm.Validation;

namespace FStorm.Test
{
    internal class TestEdmModel
    {

        [Test]
        public void It_should_init_edm() 
        {
            EdmModel edm = MockModel.PrepareModel();

            using (var sb = new StringWriter())
            {
                using (var writer = XmlWriter.Create(sb))
                {
                    IEnumerable<EdmError> errors;
                    CsdlWriter.TryWriteCsdl(edm, writer, CsdlTarget.OData, out errors);
                }
                string _expected = "<?xml version=\"1.0\" encoding=\"utf-16\"?><edmx:Edmx Version=\"4.0\" xmlns:edmx=\"http://docs.oasis-open.org/odata/ns/edmx\"><edmx:DataServices><Schema Namespace=\"my\" xmlns=\"http://docs.oasis-open.org/odata/ns/edm\"><EntityType Name=\"Customer\"><Key><PropertyRef Name=\"ID\" /></Key><Property Name=\"ID\" Type=\"Edm.Int32\" Nullable=\"false\" /><Property Name=\"RagSoc\" Type=\"Edm.String\" Nullable=\"false\" /></EntityType><EntityContainer Name=\"default\"><EntitySet Name=\"Customers\" EntityType=\"my.Customer\" /></EntityContainer></Schema></edmx:DataServices></edmx:Edmx>";
                Assert.That(sb.ToString(), Is.EqualTo(_expected));
            }

        }
    }
}
