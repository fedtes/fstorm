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
                string _expected = "<?xml version=\"1.0\" encoding=\"utf-16\"?><edmx:Edmx Version=\"4.0\" xmlns:edmx=\"http://docs.oasis-open.org/odata/ns/edmx\"><edmx:DataServices><Schema Namespace=\"my\" xmlns=\"http://docs.oasis-open.org/odata/ns/edm\"><EntityType Name=\"Customer\"><Key><PropertyRef Name=\"ID\" /></Key><Property Name=\"ID\" Type=\"Edm.Int32\" Nullable=\"false\" /><Property Name=\"RagSoc\" Type=\"Edm.String\" Nullable=\"false\" /><Property Name=\"AddressID\" Type=\"Edm.Int32\" /><NavigationProperty Name=\"Orders\" Type=\"Collection(my.Order)\"><ReferentialConstraint Property=\"CustomerID\" ReferencedProperty=\"ID\" /></NavigationProperty><NavigationProperty Name=\"Address\" Type=\"my.Address\" Nullable=\"false\" ContainsTarget=\"true\"><ReferentialConstraint Property=\"AddressID\" ReferencedProperty=\"AddressID\" /></NavigationProperty></EntityType><EntityType Name=\"Order\"><Key><PropertyRef Name=\"Number\" /></Key><Property Name=\"Number\" Type=\"Edm.String\" Nullable=\"false\" /><Property Name=\"OrderDate\" Type=\"Edm.Date\" Nullable=\"false\" /><Property Name=\"Note\" Type=\"Edm.String\" /><Property Name=\"Total\" Type=\"Edm.Decimal\" Nullable=\"false\" Scale=\"variable\" /><Property Name=\"CustomerID\" Type=\"Edm.Int32\" Nullable=\"false\" /><NavigationProperty Name=\"Customer\" Type=\"my.Customer\" Nullable=\"false\" ContainsTarget=\"true\"><ReferentialConstraint Property=\"ID\" ReferencedProperty=\"CustomerID\" /></NavigationProperty></EntityType><EntityType Name=\"Address\"><Key><PropertyRef Name=\"AddressID\" /></Key><Property Name=\"AddressID\" Type=\"Edm.Int32\" Nullable=\"false\" /><Property Name=\"City\" Type=\"Edm.String\" /><Property Name=\"Street\" Type=\"Edm.String\" /><Property Name=\"Number\" Type=\"Edm.Int32\" /><Property Name=\"Country\" Type=\"Edm.String\" /></EntityType><EntityContainer Name=\"default\"><EntitySet Name=\"Customers\" EntityType=\"my.Customer\" /><EntitySet Name=\"Orders\" EntityType=\"my.Order\" /></EntityContainer></Schema></edmx:DataServices></edmx:Edmx>";
                Assert.That(sb.ToString(), Is.EqualTo(_expected));
            }

        }
    }
}
