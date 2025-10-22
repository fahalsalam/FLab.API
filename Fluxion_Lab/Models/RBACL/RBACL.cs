namespace Fluxion_Lab.Models.RBACL
{
    public class RBACL
    {
        public class Permission
        {
            public string HeaderName { get; set; }
            public string ModuleName { get; set; }
            public string PermissionName { get; set; }
            public bool IsGranted { get; set; }
        }

        public class ModulePermissions
        {
            public string ModuleName { get; set; }
            public Dictionary<string, bool> Permissions { get; set; }
        }

        public class HeaderPermissions
        {
            public string HeaderName { get; set; }
            public List<ModulePermissions> Modules { get; set; }
        }

        public class UserPermissionsInputModel
        {
            public int UserID { get; set; }
            public List<UserModulePermissions> Modules { get; set; }
        }

        public class UserModulePermissions
        {
            public int ModuleID { get; set; }
            public List<PermissionDetail> Permissions { get; set; }
        }

        public class PermissionDetail
        {
            public string PermissionName { get; set; }
            public bool IsGranted { get; set; }
        }
    }
}
