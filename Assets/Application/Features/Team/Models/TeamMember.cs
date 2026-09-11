using System;
using UnityEngine;

namespace Assets.Application.Features.Team.Models {
    public sealed class TeamMember {
        public TeamMember(string id, string name, string role, string initials, string email, Color presence, int activeTasks) {
            Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Member id is required.", nameof(id)) : id;
            Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Member name is required.", nameof(name)) : name;
            Role = string.IsNullOrWhiteSpace(role) ? throw new ArgumentException("Role is required.", nameof(role)) : role;
            Initials = string.IsNullOrWhiteSpace(initials) ? throw new ArgumentException("Initials are required.", nameof(initials)) : initials;
            Email = string.IsNullOrWhiteSpace(email) ? throw new ArgumentException("Email is required.", nameof(email)) : email;
            Presence = presence;
            ActiveTasks = Math.Max(0, activeTasks);
        }
        public string Id { get; }
        public string Name { get; }
        public string Role { get; }
        public string Initials { get; }
        public string Email { get; }
        public Color Presence { get; }
        public int ActiveTasks { get; }
    }
}
