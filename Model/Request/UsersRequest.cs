﻿namespace Road_Infrastructure_Asset_Management.Model.Response
{
    public class UsersRequest
    {
        public string username { get; set; } = string.Empty;
        public string password_hash { get; set; } = string.Empty;
        public string full_name { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string role { get; set; } = string.Empty;
    }
}
