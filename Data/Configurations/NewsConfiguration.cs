using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Configurations
{
    public class NewsCanfiguration : IEntityTypeConfiguration<News>
    {
        public void Configure(EntityTypeBuilder<News> builder)
        {
            builder.Property(x => x.Title).IsRequired().HasMaxLength(250);
            builder.Property(x => x.Description).HasMaxLength(1000);
            builder.Property(x => x.Image).HasMaxLength(100);
        }
    }
}
