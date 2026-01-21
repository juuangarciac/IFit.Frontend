using SQLite;
using System;

namespace IFit.Models
{
    /// <summary>
    /// Representa una conversación activa entre un usuario y un coach de IA.
    /// Guarda el memoryId para mantener contexto en conversaciones futuras.
    /// </summary>
    [Table("coach_conversations")]
    public class CoachConversation
    {
        /// <summary>
        /// ID único de la conversación en la base de datos local
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>
        /// ID del usuario propietario de la conversación
        /// </summary>
        [Indexed]
        public long UserId { get; set; }

        /// <summary>
        /// Nombre del coach (ronnie, serena, eliud, kael)
        /// </summary>
        [Indexed]
        public string CoachName { get; set; } = string.Empty;

        /// <summary>
        /// Memory ID usado en el backend de IA para mantener contexto
        /// Este es el ID que se debe reutilizar para continuar la conversación
        /// </summary>
        public int MemoryId { get; set; }

        /// <summary>
        /// Fecha de creación de la conversación
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Última fecha de actualización/uso de la conversación
        /// </summary>
        public DateTime LastUsedAt { get; set; }

        /// <summary>
        /// Indica si esta conversación está activa o ha sido archivada
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Título opcional de la conversación para identificarla fácilmente
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Notas opcionales sobre el objetivo de esta conversación
        /// </summary>
        public string? Notes { get; set; }
    }
}