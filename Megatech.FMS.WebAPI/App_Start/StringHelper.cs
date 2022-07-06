namespace Megatech.FMS.WebAPI.App_Start
{
    public static class StringHelper
    {


        public static string CompoundToUnicode(this string str)
        {

            var str1 = str;
            str1 = str1.Replace("a\u0301", "\u00E1");
            str1 = str1.Replace("A\u0301", "\u00C1");
            str1 = str1.Replace("a\u0300", "\u00E0");
            str1 = str1.Replace("A\u0300", "\u00C0");
            str1 = str1.Replace("a\u0309", "\u1EA3");
            str1 = str1.Replace("A\u0309", "\u1EA2");
            str1 = str1.Replace("a\u0303", "\u00E3");
            str1 = str1.Replace("A\u0303", "\u00C3");
            str1 = str1.Replace("a\u0323", "\u1EA1");
            str1 = str1.Replace("A\u0323", "\u1EA0");
            //á Á


            str1 = str1.Replace("a\u0306\u0301", "\u1EAF");
            str1 = str1.Replace("A\u0306\u0301", "\u1EAE");
            str1 = str1.Replace("a\u0306\u0300", "\u1EB1");
            str1 = str1.Replace("A\u0306\u0300", "\u1EB0");
            str1 = str1.Replace("a\u0306\u0309", "\u1EB3");
            str1 = str1.Replace("A\u0306\u0309", "\u1EB2");
            str1 = str1.Replace("a\u0306\u0303", "\u1EB5");
            str1 = str1.Replace("A\u0306\u0303", "\u1EB4");
            str1 = str1.Replace("a\u0306\u0323", "\u1EB7");
            str1 = str1.Replace("A\u0306\u0323", "\u1EB6");
            str1 = str1.Replace("a\u0306", "\u0103");
            str1 = str1.Replace("A\u0306", "\u0102");

            str1 = str1.Replace("ă\u0301", "\u1EAF");
            str1 = str1.Replace("Ă\u0301", "\u1EAE");
            str1 = str1.Replace("ă\u0300", "\u1EB1");
            str1 = str1.Replace("Ă\u0300", "\u1EB0");
            str1 = str1.Replace("ă\u0309", "\u1EB3");
            str1 = str1.Replace("Ă\u0309", "\u1EB2");
            str1 = str1.Replace("ă\u0303", "\u1EB5");
            str1 = str1.Replace("Ă\u0303", "\u1EB4");
            str1 = str1.Replace("ă\u0323", "\u1EB7");
            str1 = str1.Replace("Ă\u0323", "\u1EB6");
            str1 = str1.Replace("a\u0306", "\u0103");
            str1 = str1.Replace("A\u0306", "\u0102");

            //a+ A+


            str1 = str1.Replace("â\u0301", "\u1EA5");
            str1 = str1.Replace("Â\u0301", "\u1EA4");
            str1 = str1.Replace("â\u0300", "\u1EA7");
            str1 = str1.Replace("Â\u0300", "\u1EA6");
            str1 = str1.Replace("â\u0309", "\u1EA9");
            str1 = str1.Replace("Â\u0309", "\u1EA8");
            str1 = str1.Replace("â\u0303", "\u1EAB");
            str1 = str1.Replace("Â\u0303", "\u1EAA");
            str1 = str1.Replace("â\u0323", "\u1EAD");
            str1 = str1.Replace("Â\u0323", "\u1EAC");
            str1 = str1.Replace("â", "\u00E2");
            str1 = str1.Replace("Â", "\u00C2");
            //"â Â




            str1 = str1.Replace("e\u0301", "\u00E9");
            str1 = str1.Replace("E\u0301", "\u00C9");
            str1 = str1.Replace("e\u0300", "\u00E8");
            str1 = str1.Replace("E\u0300", "\u00C8");
            str1 = str1.Replace("e\u0309", "\u1EBB");
            str1 = str1.Replace("E\u0309", "\u1EBA");
            str1 = str1.Replace("e\u0303", "\u1EBD");
            str1 = str1.Replace("E\u0303", "\u1EBC");
            str1 = str1.Replace("e\u0323", "\u1EB9");
            str1 = str1.Replace("E\u0323", "\u1EB8");
            //"é É



            str1 = str1.Replace("ê\u0301", "\u1EBF");
            str1 = str1.Replace("Ê\u0301", "\u1EBE");
            str1 = str1.Replace("ê\u0300", "\u1EC1");
            str1 = str1.Replace("Ê\u0300", "\u1EC0");
            str1 = str1.Replace("ê\u0309", "\u1EC3");
            str1 = str1.Replace("Ê\u0309", "\u1EC2");
            str1 = str1.Replace("ê\u0303", "\u1EC5");
            str1 = str1.Replace("Ê\u0303", "\u1EC4");
            str1 = str1.Replace("ê\u0323", "\u1EC7");
            str1 = str1.Replace("Ê\u0323", "\u1EC6");
            str1 = str1.Replace("ê", "\u00EA");
            str1 = str1.Replace("Ê", "\u00CA");



            str1 = str1.Replace("i\u0301", "\u00ED");
            str1 = str1.Replace("I\u0301", "\u00CD");
            str1 = str1.Replace("i\u0300", "\u00EC");
            str1 = str1.Replace("I\u0300", "\u00CC");
            str1 = str1.Replace("i\u0309", "\u1EC9");
            str1 = str1.Replace("I\u0309", "\u1EC8");
            str1 = str1.Replace("i\u0303", "\u0129");
            str1 = str1.Replace("I\u0303", "\u0128");
            str1 = str1.Replace("i\u0323", "\u1ECB");
            str1 = str1.Replace("I\u0323", "\u1ECA");
            //"í Í




            str1 = str1.Replace("o\u0301", "\u00F3");
            str1 = str1.Replace("O\u0301", "\u00D3");
            str1 = str1.Replace("o\u0300", "\u00F2");
            str1 = str1.Replace("O\u0300", "\u00D2");
            str1 = str1.Replace("o\u0309", "\u1ECF");
            str1 = str1.Replace("O\u0309", "\u1ECE");
            str1 = str1.Replace("o\u0303", "\u00F5");
            str1 = str1.Replace("O\u0303", "\u00D5");
            str1 = str1.Replace("o\u0323", "\u1ECD");
            str1 = str1.Replace("O\u0323", "\u1ECC");
            //"ó Ó




            str1 = str1.Replace("ơ\u0301", "\u1EDB");
            str1 = str1.Replace("Ơ\u0301", "\u1EDA");
            str1 = str1.Replace("ơ\u0300", "\u1EDD");
            str1 = str1.Replace("Ơ\u0300", "\u1EDC");
            str1 = str1.Replace("ơ\u0309", "\u1EDF");
            str1 = str1.Replace("Ơ\u0309", "\u1EDE");
            str1 = str1.Replace("ơ\u0303", "\u1EE1");
            str1 = str1.Replace("Ơ\u0303", "\u1EE0");
            str1 = str1.Replace("ơ\u0323", "\u1EE3");
            str1 = str1.Replace("Ơ\u0323", "\u1EE2");
            str1 = str1.Replace("ơ", "\u01A1");
            str1 = str1.Replace("Ơ", "\u01A0");
            //"o+ O+



            str1 = str1.Replace("ô\u0301", "\u1ED1");
            str1 = str1.Replace("Ô\u0301", "\u1ED0");
            str1 = str1.Replace("ô\u0300", "\u1ED3");
            str1 = str1.Replace("Ô\u0300", "\u1ED2");
            str1 = str1.Replace("ô\u0309", "\u1ED5");
            str1 = str1.Replace("Ô\u0309", "\u1ED4");
            str1 = str1.Replace("ô\u0303", "\u1ED7");
            str1 = str1.Replace("Ô\u0303", "\u1ED6");
            str1 = str1.Replace("ô\u0323", "\u1ED9");
            str1 = str1.Replace("Ô\u0323", "\u1ED8");
            str1 = str1.Replace("ô", "\u00F4");
            str1 = str1.Replace("Ô", "\u00D4");
            //"ô Ô




            str1 = str1.Replace("u\u0301", "\u00FA");
            str1 = str1.Replace("U\u0301", "\u00DA");
            str1 = str1.Replace("u\u0300", "\u00F9");
            str1 = str1.Replace("U\u0300", "\u00D9");
            str1 = str1.Replace("u\u0309", "\u1EE7");
            str1 = str1.Replace("U\u0309", "\u1EE6");
            str1 = str1.Replace("u\u0303", "\u0169");
            str1 = str1.Replace("U\u0303", "\u0168");
            str1 = str1.Replace("u\u0323", "\u1EE5");
            str1 = str1.Replace("U\u0323", "\u1EE4");
            //"ú Ú




            str1 = str1.Replace("ư\u0301", "\u1EE9");
            str1 = str1.Replace("Ư\u0301", "\u1EE8");
            str1 = str1.Replace("ư\u0300", "\u1EEB");
            str1 = str1.Replace("Ư\u0300", "\u1EEA");
            str1 = str1.Replace("ư\u0309", "\u1EED");
            str1 = str1.Replace("Ư\u0309", "\u1EEC");
            str1 = str1.Replace("ư\u0303", "\u1EEF");
            str1 = str1.Replace("Ư\u0303", "\u1EEE");
            str1 = str1.Replace("ư\u0323", "\u1EF1");
            str1 = str1.Replace("Ư\u0323", "\u1EF0");
            str1 = str1.Replace("ư", "\u01B0");
            str1 = str1.Replace("Ư", "\u01AF");
            //"u+ U+


            str1 = str1.Replace("y\u0301", "\u00FD");
            str1 = str1.Replace("Y\u0301", "\u00DD");
            str1 = str1.Replace("y\u0300", "\u1EF3");
            str1 = str1.Replace("Y\u0300", "\u1EF2");
            str1 = str1.Replace("y\u0309", "\u1EF7");
            str1 = str1.Replace("Y\u0309", "\u1EF6");
            str1 = str1.Replace("y\u0303", "\u1EF9");
            str1 = str1.Replace("Y\u0303", "\u1EF8");
            str1 = str1.Replace("y\u0323", "\u1EF5");
            str1 = str1.Replace("Y\u0323", "\u1EF4");
            //"ý Ý


            return str1;

        }
    }
}