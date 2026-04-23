using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediMateRepository.Migrations
{
    /// <inheritdoc />
    public partial class deletemodelsnotuse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatbotMessages");

            migrationBuilder.DropTable(
                name: "RagBaseConfigs");

            migrationBuilder.DropTable(
                name: "RagBaseEmbeddings");

            migrationBuilder.DropTable(
                name: "ChatbotSessions");

            migrationBuilder.DropTable(
                name: "RagBaseDocuments");

            migrationBuilder.DropTable(
                name: "RagBaseCollections");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatbotSessions",
                columns: table => new
                {
                    BotSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastMessageAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    SessionTitle = table.Column<string>(type: "text", nullable: false),
                    StartAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatbotSessions", x => x.BotSessionId);
                    table.ForeignKey(
                        name: "FK_ChatbotSessions_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "MemberId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RagBaseCollections",
                columns: table => new
                {
                    CollectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RagBaseCollections", x => x.CollectionId);
                });

            migrationBuilder.CreateTable(
                name: "RagBaseConfigs",
                columns: table => new
                {
                    ConfigId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChunkOverlap = table.Column<int>(type: "integer", nullable: false),
                    ChunkSize = table.Column<int>(type: "integer", nullable: false),
                    ContextWindow = table.Column<int>(type: "integer", nullable: false),
                    EmbeddingModel = table.Column<string>(type: "text", nullable: false),
                    IsUseApi = table.Column<bool>(type: "boolean", nullable: false),
                    LLMModel = table.Column<string>(type: "text", nullable: false),
                    MaxTokens = table.Column<int>(type: "integer", nullable: false),
                    PromptTemplate = table.Column<string>(type: "text", nullable: false),
                    ResponseType = table.Column<string>(type: "text", nullable: false),
                    Temperature = table.Column<float>(type: "real", nullable: false),
                    TopK = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RagBaseConfigs", x => x.ConfigId);
                });

            migrationBuilder.CreateTable(
                name: "ChatbotMessages",
                columns: table => new
                {
                    BotMessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    BotSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CreateAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    Role = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatbotMessages", x => x.BotMessageId);
                    table.ForeignKey(
                        name: "FK_ChatbotMessages_ChatbotSessions_BotSessionId",
                        column: x => x.BotSessionId,
                        principalTable: "ChatbotSessions",
                        principalColumn: "BotSessionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RagBaseDocuments",
                columns: table => new
                {
                    RagDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CollectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CheckSum = table.Column<string>(type: "text", nullable: false),
                    CreateAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DocName = table.Column<string>(type: "text", nullable: false),
                    FilePath = table.Column<string>(type: "text", nullable: false),
                    FileSize = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RagBaseDocuments", x => x.RagDocumentId);
                    table.ForeignKey(
                        name: "FK_RagBaseDocuments_RagBaseCollections_CollectionId",
                        column: x => x.CollectionId,
                        principalTable: "RagBaseCollections",
                        principalColumn: "CollectionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RagBaseEmbeddings",
                columns: table => new
                {
                    EmbeddingId = table.Column<Guid>(type: "uuid", nullable: false),
                    RagDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChunkSize = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Embedding = table.Column<float[]>(type: "real[]", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Metadata = table.Column<string>(type: "text", nullable: false),
                    NodeId = table.Column<string>(type: "text", nullable: false),
                    ParentNodeId = table.Column<string>(type: "text", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RagBaseEmbeddings", x => x.EmbeddingId);
                    table.ForeignKey(
                        name: "FK_RagBaseEmbeddings_RagBaseDocuments_RagDocumentId",
                        column: x => x.RagDocumentId,
                        principalTable: "RagBaseDocuments",
                        principalColumn: "RagDocumentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatbotMessages_BotSessionId",
                table: "ChatbotMessages",
                column: "BotSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatbotSessions_MemberId",
                table: "ChatbotSessions",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_RagBaseDocuments_CollectionId",
                table: "RagBaseDocuments",
                column: "CollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_RagBaseEmbeddings_RagDocumentId",
                table: "RagBaseEmbeddings",
                column: "RagDocumentId");
        }
    }
}
