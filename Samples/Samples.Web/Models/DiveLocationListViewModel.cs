using Microsoft.AspNetCore.Mvc;
using RCommon.Collections;
using RCommon.Models;
using Samples.Application.Contracts.Dto;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Samples.Web.Models
{
    public class DiveLocationListViewModel
    {
        public string DisplayMessage { get; set; }


        [BindProperty(SupportsGet = true)]
        public int CurrentPage
        {
            get; set;

        }


        public int Count => this.PagedData.TotalCount;
        public int PageSize
        {
            get
            {
                if (this.PagedData == null)
                {
                    return PresentationDefaults.PagedDataSize;
                }
                else
                {
                    return this.PagedData.PageSize.Value;
                }


            }
        }

        public int TotalPages => this.PagedData.TotalPages;

        public DiveLocationListModel PagedData { get; set; }

        public bool ShowPrevious => this.PagedData.HasPreviousPage;
        public bool ShowNext => this.PagedData.HasNextPage;
        public bool ShowFirst => CurrentPage != 1;
        public bool ShowLast => CurrentPage != TotalPages;

        [Display(Name = "Search Terms")]
        public string SearchTerms { get; set; }
    }
}
