using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace IFit.Models.Dtos
{
    namespace IFit.Models.Dtos
    {
        /// <summary>
        /// DTO genérico para respuestas paginadas de Spring Boot
        /// </summary>
        public class PagedResponse<T>
        {
            [JsonPropertyName("content")]
            public List<T> Content { get; set; } = new();

            [JsonPropertyName("pageable")]
            public PageableInfo Pageable { get; set; } = new();

            [JsonPropertyName("totalPages")]
            public int TotalPages { get; set; }

            [JsonPropertyName("totalElements")]
            public long TotalElements { get; set; }

            [JsonPropertyName("last")]
            public bool Last { get; set; }

            [JsonPropertyName("size")]
            public int Size { get; set; }

            [JsonPropertyName("number")]
            public int Number { get; set; }

            [JsonPropertyName("numberOfElements")]
            public int NumberOfElements { get; set; }

            [JsonPropertyName("first")]
            public bool First { get; set; }

            [JsonPropertyName("empty")]
            public bool Empty { get; set; }
        }

        public class PageableInfo
        {
            [JsonPropertyName("sort")]
            public SortInfo Sort { get; set; } = new();

            [JsonPropertyName("offset")]
            public long Offset { get; set; }

            [JsonPropertyName("pageNumber")]
            public int PageNumber { get; set; }

            [JsonPropertyName("pageSize")]
            public int PageSize { get; set; }

            [JsonPropertyName("paged")]
            public bool Paged { get; set; }

            [JsonPropertyName("unpaged")]
            public bool Unpaged { get; set; }
        }

        public class SortInfo
        {
            [JsonPropertyName("empty")]
            public bool Empty { get; set; }

            [JsonPropertyName("sorted")]
            public bool Sorted { get; set; }

            [JsonPropertyName("unsorted")]
            public bool Unsorted { get; set; }
        }
    }
}
