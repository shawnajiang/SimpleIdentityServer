﻿using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using SimpleIdentityServer.Uma.EF;

namespace SimpleIdentityServer.Uma.EF.Migrations
{
    [DbContext(typeof(SimpleIdServerUmaContext))]
    partial class SimpleIdServerUmaContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "1.0.0-rc2-20901")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("SimpleIdentityServer.Uma.EF.Models.Policy", b =>
                {
                    b.Property<string>("Id");

                    b.Property<string>("Claims");

                    b.Property<string>("ClientIdsAllowed");

                    b.Property<bool>("IsResourceOwnerConsentNeeded");

                    b.Property<string>("Scopes");

                    b.Property<string>("Script");

                    b.HasKey("Id");

                    b.ToTable("Policies");
                });

            modelBuilder.Entity("SimpleIdentityServer.Uma.EF.Models.ResourceSet", b =>
                {
                    b.Property<string>("Id");

                    b.Property<string>("IconUri");

                    b.Property<string>("Name");

                    b.Property<string>("PolicyId");

                    b.Property<string>("Scopes");

                    b.Property<string>("Type");

                    b.Property<string>("Uri");

                    b.HasKey("Id");

                    b.HasIndex("PolicyId");

                    b.ToTable("ResourceSets");
                });

            modelBuilder.Entity("SimpleIdentityServer.Uma.EF.Models.Rpt", b =>
                {
                    b.Property<string>("Value");

                    b.Property<DateTime>("CreateDateTime");

                    b.Property<DateTime>("ExpirationDateTime");

                    b.Property<string>("ResourceSetId");

                    b.Property<string>("TicketId");

                    b.HasKey("Value");

                    b.HasIndex("ResourceSetId");

                    b.HasIndex("TicketId");

                    b.ToTable("Rpts");
                });

            modelBuilder.Entity("SimpleIdentityServer.Uma.EF.Models.Scope", b =>
                {
                    b.Property<string>("Id");

                    b.Property<string>("IconUri");

                    b.Property<string>("Name");

                    b.HasKey("Id");

                    b.ToTable("Scopes");
                });

            modelBuilder.Entity("SimpleIdentityServer.Uma.EF.Models.Ticket", b =>
                {
                    b.Property<string>("Id");

                    b.Property<string>("ClientId");

                    b.Property<DateTime>("CreateDateTime");

                    b.Property<DateTime>("ExpirationDateTime");

                    b.Property<string>("ResourceSetId");

                    b.Property<string>("Scopes");

                    b.HasKey("Id");

                    b.HasIndex("ResourceSetId");

                    b.ToTable("Tickets");
                });

            modelBuilder.Entity("SimpleIdentityServer.Uma.EF.Models.ResourceSet", b =>
                {
                    b.HasOne("SimpleIdentityServer.Uma.EF.Models.Policy")
                        .WithMany()
                        .HasForeignKey("PolicyId");
                });

            modelBuilder.Entity("SimpleIdentityServer.Uma.EF.Models.Rpt", b =>
                {
                    b.HasOne("SimpleIdentityServer.Uma.EF.Models.ResourceSet")
                        .WithMany()
                        .HasForeignKey("ResourceSetId");

                    b.HasOne("SimpleIdentityServer.Uma.EF.Models.Ticket")
                        .WithMany()
                        .HasForeignKey("TicketId");
                });

            modelBuilder.Entity("SimpleIdentityServer.Uma.EF.Models.Ticket", b =>
                {
                    b.HasOne("SimpleIdentityServer.Uma.EF.Models.ResourceSet")
                        .WithMany()
                        .HasForeignKey("ResourceSetId");
                });
        }
    }
}
