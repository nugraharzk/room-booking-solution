CREATE TABLE "Rooms" (
    "Id" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Location" character varying(200),
    "Capacity" integer NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_Rooms" PRIMARY KEY ("Id")
);


CREATE TABLE "Users" (
    "Id" uuid NOT NULL,
    "Email" character varying(200) NOT NULL,
    "PasswordHash" text NOT NULL,
    "FirstName" character varying(100) NOT NULL,
    "LastName" character varying(100) NOT NULL,
    "Role" character varying(50) NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_Users" PRIMARY KEY ("Id")
);


CREATE TABLE "Bookings" (
    "Id" uuid NOT NULL,
    "RoomId" uuid NOT NULL,
    "CreatedByUserId" uuid NOT NULL,
    "Subject" character varying(200),
    "StartAt" timestamp with time zone NOT NULL,
    "EndAt" timestamp with time zone NOT NULL,
    "Status" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "StatusChangedAt" timestamp with time zone,
    CONSTRAINT "PK_Bookings" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Bookings_Rooms_RoomId" FOREIGN KEY ("RoomId") REFERENCES "Rooms" ("Id") ON DELETE RESTRICT
);


CREATE INDEX "IX_Bookings_RoomId_Status" ON "Bookings" ("RoomId", "Status");


CREATE INDEX "IX_Rooms_IsActive" ON "Rooms" ("IsActive");


CREATE UNIQUE INDEX "IX_Rooms_Name" ON "Rooms" ("Name");


CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" ("Email");


