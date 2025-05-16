using System.Collections.Generic;
using System.Linq;  // เพิ่มการใช้ LINQ
using System;

namespace KAEAGoalWebAPI.Models
{
    public class PaginatedList<T>
    {
        public List<T> Items { get; set; }
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }

        public PaginatedList(List<T> items, int totalCount, int currentPage, int pageSize, int totalPages)
        {
            Items = items;
            TotalCount = totalCount;
            CurrentPage = currentPage;
            PageSize = pageSize;
            TotalPages = totalPages;
        }

        public static PaginatedList<T> Create(List<T> source, int count, int pageNumber, int pageSize)
        {
            var totalPages = (int)Math.Ceiling(count / (double)pageSize);

            // แปลง List<T> ให้เป็น IEnumerable<T> เพื่อรองรับ Skip และ Take
            var items = source.AsEnumerable()
                              .Skip((pageNumber - 1) * pageSize)
                              .Take(pageSize)
                              .ToList();

            return new PaginatedList<T>(items, count, pageNumber, pageSize, totalPages);
        }
    }
}
