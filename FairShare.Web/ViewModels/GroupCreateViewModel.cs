namespace FairShare.Web.ViewModels
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using Microsoft.AspNetCore.Mvc.Rendering;

    public class GroupCreateViewModel
    {
        [Required, StringLength(120)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public List<int> SelectedUserIds { get; set; } = new();

        public List<SelectListItem> AllUsers { get; set; } = new();
    }
}
