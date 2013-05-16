﻿using DDD.Light.Realtor.Domain.Model;

namespace DDD.Light.Realtor.Domain.Events
{
    public class ListingPosted
    {
        public Model.Realtor Realtor { get; set; }
        public Listing Listing { get; set; }
    }
}