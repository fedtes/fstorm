using System.Xml;
using Microsoft.Extensions.DependencyInjection;
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
            var services = new ServiceCollection();
            services.AddFStorm(new ODataOptions() { SQLCompilerType= SQLCompilerType.MSSQL , ServiceRoot= "https://my.service/odata/"});
            var serviceProvider = services.BuildServiceProvider();
            var odataService = serviceProvider.GetService<ODataService>()!;
            MockModel.PrepareModel(odataService.CreateNewModel());

            using (var sb = new StringWriter())
            {
                using (var writer = XmlWriter.Create(sb))
                {
                    IEnumerable<EdmError> errors;
                    CsdlWriter.TryWriteCsdl(odataService.Model, writer, CsdlTarget.OData, out errors);
                }
                string _current = sb.ToString();
                string _expected = "<?xml version=\"1.0\" encoding=\"utf-16\"?><edmx:Edmx Version=\"4.0\" xmlns:edmx=\"http://docs.oasis-open.org/odata/ns/edmx\"><edmx:DataServices><Schema Namespace=\"my\" xmlns=\"http://docs.oasis-open.org/odata/ns/edm\"><EntityType Name=\"Customer\"><Key><PropertyRef Name=\"ID\" /></Key><Property Name=\"ID\" Type=\"Edm.Int32\" Nullable=\"false\" /><Property Name=\"RagSoc\" Type=\"Edm.String\" Nullable=\"false\" /><Property Name=\"AddressID\" Type=\"Edm.Int32\" /><NavigationProperty Name=\"Orders\" Type=\"Collection(my.Order)\"><ReferentialConstraint Property=\"ID\" ReferencedProperty=\"CustomerID\" /></NavigationProperty><NavigationProperty Name=\"Address\" Type=\"my.Address\" Nullable=\"false\" ContainsTarget=\"true\"><ReferentialConstraint Property=\"AddressID\" ReferencedProperty=\"AddressID\" /></NavigationProperty></EntityType><EntityType Name=\"Order\"><Key><PropertyRef Name=\"Number\" /></Key><Property Name=\"Number\" Type=\"Edm.String\" Nullable=\"false\" /><Property Name=\"OrderDate\" Type=\"Edm.String\" Nullable=\"false\" /><Property Name=\"Note\" Type=\"Edm.String\" /><Property Name=\"Total\" Type=\"Edm.Decimal\" Nullable=\"false\" Scale=\"variable\" /><Property Name=\"DeliveryAddressID\" Type=\"Edm.Int32\" /><Property Name=\"CustomerID\" Type=\"Edm.Int32\" Nullable=\"false\" /><NavigationProperty Name=\"Customer\" Type=\"my.Customer\" Nullable=\"false\" ContainsTarget=\"true\"><ReferentialConstraint Property=\"CustomerID\" ReferencedProperty=\"ID\" /></NavigationProperty><NavigationProperty Name=\"Articles\" Type=\"Collection(my.Article)\"><ReferentialConstraint Property=\"Number\" ReferencedProperty=\"Number\" /></NavigationProperty><NavigationProperty Name=\"DeliveryAddress\" Type=\"my.Address\"><ReferentialConstraint Property=\"DeliveryAddressID\" ReferencedProperty=\"AddressID\" /></NavigationProperty></EntityType><EntityType Name=\"Article\"><Key><PropertyRef Name=\"ArticleId\" /></Key><Property Name=\"ArticleId\" Type=\"Edm.Int32\" Nullable=\"false\" /><Property Name=\"Name\" Type=\"Edm.String\" Nullable=\"false\" /><Property Name=\"Prize\" Type=\"Edm.Decimal\" Nullable=\"false\" Scale=\"variable\" /><Property Name=\"Number\" Type=\"Edm.String\" Nullable=\"false\" /><NavigationProperty Name=\"Order\" Type=\"my.Order\" Nullable=\"false\" ContainsTarget=\"true\"><ReferentialConstraint Property=\"Number\" ReferencedProperty=\"Number\" /></NavigationProperty></EntityType><EntityType Name=\"Address\"><Key><PropertyRef Name=\"AddressID\" /></Key><Property Name=\"AddressID\" Type=\"Edm.Int32\" Nullable=\"false\" /><Property Name=\"City\" Type=\"Edm.String\" /><Property Name=\"Street\" Type=\"Edm.String\" /><Property Name=\"Number\" Type=\"Edm.Int32\" /><Property Name=\"Country\" Type=\"Edm.String\" /><NavigationProperty Name=\"Hints\" Type=\"Collection(my.AddressHint)\"><ReferentialConstraint Property=\"AddressID\" ReferencedProperty=\"AddressID\" /></NavigationProperty></EntityType><EntityType Name=\"AddressHint\"><Key><PropertyRef Name=\"AddressHintID\" /></Key><Property Name=\"AddressHintID\" Type=\"Edm.Int32\" Nullable=\"false\" /><Property Name=\"AddressID\" Type=\"Edm.Int32\" Nullable=\"false\" /><Property Name=\"Hint\" Type=\"Edm.String\" Nullable=\"false\" /></EntityType><EntityContainer Name=\"default\"><EntitySet Name=\"Customers\" EntityType=\"my.Customer\" /><EntitySet Name=\"Orders\" EntityType=\"my.Order\" /><EntitySet Name=\"Adresses\" EntityType=\"my.Address\" /><EntitySet Name=\"AddressHints\" EntityType=\"my.AddressHint\" /><EntitySet Name=\"Articles\" EntityType=\"my.Article\" /></EntityContainer></Schema></edmx:DataServices></edmx:Edmx>";
                Assert.That(_current, Is.EqualTo(_expected));
            }

        }
    }
}
