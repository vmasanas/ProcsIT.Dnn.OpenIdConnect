using System;
using System.Runtime.Serialization;

namespace ProcsIT.Dnn.AuthServices.OpenIdConnect
{
    [DataContract]
    public class UserData
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "family_name")]
        public string FamilyName { get; set; }

        [DataMember(Name = "given_name")]
        public string GivenName { get; set; }

        [DataMember(Name = "middle_name")]
        public string MiddleName { get; set; }

        [DataMember(Name = "nickname")]
        public string NickName { get; set; }

        [DataMember(Name = "preferred_username")]
        public string PreferredUserName { get; set; }

        [DataMember(Name = "profile")]
        public string Profile { get; set; }

        [DataMember(Name = "picture")]
        public string Picture { get; set; }

        [DataMember(Name = "website")]
        public string Website { get; set; }

        [DataMember(Name = "gender")]
        public string Gender { get; set; }

        [DataMember(Name = "birthdate")]
        public virtual string BirthDate { get; set; }

        [DataMember(Name = "zoneinfo")]
        public virtual string ZoneInfo { get; set; }

        [DataMember(Name = "locale")]
        public virtual string Locale { get; set; }

        [DataMember(Name = "updated_at")]
        public virtual string UpdatedAt { get; set; }


        [DataMember(Name = "username")]
        public virtual string UserName { get; set; }


        public virtual string DisplayName
        {
            get
            {
                return Name;
            }
            set { }
        }

        //[DataMember(Name = "email")]
        //public virtual string Email { get; set; }

        //[DataMember(Name = "emails")]
        //public EmailData Emails { get; set; }

        public virtual string FirstName
        {
            get
            {
                return (!String.IsNullOrEmpty(Name) && Name.IndexOf(" ", StringComparison.Ordinal) > 0) ? Name.Substring(0, Name.IndexOf(" ", StringComparison.Ordinal)) : String.Empty;
            }
            set { Name = value + " " + LastName; }
        }

        public virtual string LastName
        {
            get
            {
                return (!String.IsNullOrEmpty(Name) && Name.IndexOf(" ", StringComparison.Ordinal) > 0) ? Name.Substring(Name.IndexOf(" ", StringComparison.Ordinal) + 1) : Name;
            }
            set { Name = FirstName + " " + value; }

        }


        //public string PreferredEmail 
        //{ 
        //    get
        //    {
        //        if (Emails == null)
        //        {
        //            return Email;
        //        }
        //        return Emails.PreferredEmail;
        //    }
        //}

        //public virtual string ProfileImage { get; set; }

        //[DataMember(Name = "timezone")]
        //public string Timezone { get; set; }

        //[DataMember(Name = "time_zone")]
        //public string TimeZoneInfo { get; set; }
    }

    //[DataContract]
    //public class EmailData
    //{
    //    [DataMember(Name = "preferred")]
    //    public string PreferredEmail { get; set; }

    //    [DataMember(Name = "account")]
    //    public string AccountEmail { get; set; }

    //    [DataMember(Name = "personal")]
    //    public string PersonalEmail { get; set; }

    //    [DataMember(Name = "business")]
    //    public string BusinessEmail { get; set; }
    //}
}