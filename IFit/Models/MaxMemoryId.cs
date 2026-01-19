using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IFit.Models
{
    public class MaxMemoryId
    {
        [JsonPropertyName("maxMemoryId")]
        public int Id { get; set; }

        public MaxMemoryId()
        {
            Id = 0;
        }
    }
}
