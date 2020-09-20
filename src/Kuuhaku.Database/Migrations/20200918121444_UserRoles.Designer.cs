﻿// <auto-generated />
using System;
using Kuuhaku.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Kuuhaku.Database.Migrations
{
    [DbContext(typeof(DisgustingGodContext))]
    [Migration("20200918121444_UserRoles")]
    partial class UserRoles
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.7")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("Kuuhaku.Database.DbModels.GuildConfig", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<string>("CommandSeperator")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<ulong>("GuildId")
                        .HasColumnType("bigint unsigned");

                    b.Property<string>("Prefix")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.HasKey("Id");

                    b.ToTable("GuildConfigs");
                });

            modelBuilder.Entity("Kuuhaku.Database.DbModels.Reminder", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<ulong>("ChannelId")
                        .HasColumnType("bigint unsigned");

                    b.Property<string>("Contents")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<ulong?>("GuildId")
                        .HasColumnType("bigint unsigned");

                    b.Property<bool>("IsActive")
                        .HasColumnType("tinyint(1)");

                    b.Property<DateTime>("RemindAt")
                        .HasColumnType("datetime(6)");

                    b.Property<ulong>("UserId")
                        .HasColumnType("bigint unsigned");

                    b.HasKey("Id");

                    b.ToTable("Reminders");
                });

            modelBuilder.Entity("Kuuhaku.Database.DbModels.UserRole", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<ulong?>("EmojiId")
                        .HasColumnType("bigint unsigned");

                    b.Property<string>("EmojiName")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<ulong?>("MessageId")
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong>("RoleId")
                        .HasColumnType("bigint unsigned");

                    b.Property<string>("ShortDescription")
                        .HasColumnType("varchar(200) CHARACTER SET utf8mb4")
                        .HasMaxLength(200);

                    b.Property<Guid?>("UserRoleLocationId")
                        .HasColumnType("char(36)");

                    b.HasKey("Id");

                    b.HasIndex("UserRoleLocationId");

                    b.ToTable("UserRoles");
                });

            modelBuilder.Entity("Kuuhaku.Database.DbModels.UserRoleLocation", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<ulong>("ChannelId")
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong>("GuildId")
                        .HasColumnType("bigint unsigned");

                    b.HasKey("Id");

                    b.ToTable("UserRoleLocations");
                });

            modelBuilder.Entity("Kuuhaku.Database.DbModels.UserRole", b =>
                {
                    b.HasOne("Kuuhaku.Database.DbModels.UserRoleLocation", null)
                        .WithMany("Roles")
                        .HasForeignKey("UserRoleLocationId");
                });
#pragma warning restore 612, 618
        }
    }
}
