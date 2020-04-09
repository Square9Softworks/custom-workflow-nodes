using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SharePointRelease
{
    [DesignerCategory("code"), XmlRoot(Namespace = "", IsNullable = false), XmlType(AnonymousType = true)]
    [Serializable]
    public class PropertyMapping
    {
        private PropertyMappingConfig configField;

        private PropertyMappingField[] fieldsField;

        public PropertyMappingConfig Config
        {
            get
            {
                return this.configField;
            }
            set
            {
                this.configField = value;
            }
        }

        [XmlArrayItem("Field", IsNullable = false)]
        public PropertyMappingField[] Fields
        {
            get
            {
                return this.fieldsField;
            }
            set
            {
                this.fieldsField = value;
            }
        }
    }

    [DesignerCategory("code"), XmlType(AnonymousType = true)]
    [Serializable]
    public class PropertyMappingConfig
    {
        private string webURLField;

        private string destinationURLField;

        private string domainField;

        private string usernameField;

        private string passwordField;

        private bool folderCreationField;

        private string folderField;

        private string fileNameField;

        private string instanceTypeField;

        private string processIdFieldField;

        public string WebURL
        {
            get
            {
                return this.webURLField;
            }
            set
            {
                this.webURLField = value;
            }
        }

        public string DestinationURL
        {
            get
            {
                return this.destinationURLField;
            }
            set
            {
                this.destinationURLField = value;
            }
        }

        public string FolderField
        {
            get
            {
                return this.folderField;
            }
            set
            {
                this.folderField = value;
            }
        }

        public string FileNameField
        {
            get
            {
                return this.fileNameField;
            }
            set
            {
                this.fileNameField = value;
            }
        }

        public string Domain
        {
            get
            {
                return this.domainField;
            }
            set
            {
                this.domainField = value;
            }
        }

        public string Username
        {
            get
            {
                return this.usernameField;
            }
            set
            {
                this.usernameField = value;
            }
        }

        public string Password
        {
            get
            {
                return this.passwordField;
            }
            set
            {
                this.passwordField = value;
            }
        }

        public bool FolderCreation
        {
            get
            {
                return this.folderCreationField;
            }
            set
            {
                this.folderCreationField = value;
            }
        }

        public string InstanceType
        {
            get
            {
                return this.instanceTypeField;
            }
            set
            {
                this.instanceTypeField = value;
            }
        }

        public string ProcessIdField
        {
            get
            {
                return this.processIdFieldField;
            }
            set
            {
                this.processIdFieldField = value;
            }
        }
    }

    [DesignerCategory("code"), XmlType(AnonymousType = true)]
    [Serializable]
    public class PropertyMappingField
    {
        private int idField;

        private string valueField;

        [XmlAttribute]
        public int ID
        {
            get
            {
                return this.idField;
            }
            set
            {
                this.idField = value;
            }
        }

        [XmlText]
        public string Value
        {
            get
            {
                return this.valueField;
            }
            set
            {
                this.valueField = value;
            }
        }
    }
}