using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using RestaurantManagement.Shared.Models;

namespace RestaurantManagement.Modules.Menu.Models
{
    public class CreateMenuRequest
    {
        [RequiredId(ErrorMessage = "RequiredAttribute_ValidationError")]
        [JsonConverter(typeof(LenientGuidConverter))]
        public Guid Id { get; set; }

        [Required(ErrorMessage = "RequiredAttribute_ValidationError")]
        [MaxLength(100, ErrorMessage = "MaxLengthAttribute_ValidationError")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500, ErrorMessage = "MaxLengthAttribute_ValidationError")]
        public string? Description { get; set; }

        public List<Guid>? LocationIds { get; set; }
    }
}
