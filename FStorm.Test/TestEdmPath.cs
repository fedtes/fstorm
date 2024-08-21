using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FStorm.Test
{
    internal class TestEdmPath
    {
        [Test]
        public void It_should_create_empty_path()
        {
            var path = new EdmPath();
            Assert.False(path.HasValue());
            Assert.That(path.Count(), Is.EqualTo(0));
        }


        [Test]
        public void It_should_create_1_entity_path()
        {
            var path = new EdmPath("MyEntity");
            Assert.True(path.HasValue());
            Assert.That(path.GetEntityName(), Is.EqualTo("MyEntity"));
        }

        [Test]
        public void It_should_create_1_entity_withId_path()
        {
            var path = new EdmPath("MyEntity(5)");
            Assert.True(path.HasValue());
            Assert.True(path.HasId());
            Assert.That(path.GetEntityName(), Is.EqualTo("MyEntity"));
            Assert.That(path.GetId(), Is.EqualTo("5"));
        }

        [Test]
        public void It_should_create_1_entity_with_quoted_Id_path()
        {
            var path = new EdmPath("My_Entity('abcde')");
            Assert.True(path.HasValue());
            Assert.True(path.HasId());
            Assert.That(path.GetEntityName(), Is.EqualTo("My_Entity"));
            Assert.That(path.GetId(), Is.EqualTo("abcde"));
        }

        [Test]
        public void It_should_create_complete_path()
        {
            var path = new EdmPath("My_Entity('abcde')", "SubEntity");
            Assert.False(path.HasValue());
            Assert.False(path.HasId());
            Assert.That(path.Count(), Is.EqualTo(2));
            Assert.That(path.ToString(), Is.EqualTo("My_Entity('abcde')/SubEntity"));
        }


        [Test]
        public void It_should_combine_path()
        {
            var path = new EdmPath("My_Entity('abcde')") + new EdmPath("SubEntity");
            Assert.False(path.HasValue());
            Assert.False(path.HasId());
            Assert.That(path.Count(), Is.EqualTo(2));
            Assert.That(path.ToString(), Is.EqualTo("My_Entity('abcde')/SubEntity"));
        }


        [Test]
        public void It_should_combine_path_2()
        {
            var path = new EdmPath("My_Entity('abcde')", "SubEntity") + new EdmPath("SubEntity2");
            Assert.False(path.HasValue());
            Assert.False(path.HasId());
            Assert.That(path.Count(), Is.EqualTo(3));
            Assert.That(path.ToString(), Is.EqualTo("My_Entity('abcde')/SubEntity/SubEntity2"));
        }

    }
}
