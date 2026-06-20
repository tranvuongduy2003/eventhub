import { readFileSync } from "node:fs";
import { resolve } from "node:path";

const DATA_DIR = resolve(__dirname, "../../src/DataSeeder/Data");

export interface SeedUser {
  id: string;
  displayName: string;
  email: string;
  role: "Organizer" | "Attendee";
}

export interface SeedRole {
  name: string;
  description: string;
  permissions: string[];
}

export interface SeedPermission {
  name: string;
  description: string;
}

export interface SeedEventUserRole {
  eventId: number;
  userId: string;
  role: "Owner" | "Staff";
}

function loadJson<T>(filename: string): T {
  return JSON.parse(readFileSync(resolve(DATA_DIR, filename), "utf8")) as T;
}

export const users: SeedUser[] = loadJson<SeedUser[]>("Users.json");
export const roles: SeedRole[] = loadJson<SeedRole[]>("Roles.json");
export const permissions: SeedPermission[] =
  loadJson<SeedPermission[]>("Permissions.json");
export const eventUserRoles: SeedEventUserRole[] =
  loadJson<SeedEventUserRole[]>("EventUserRoles.json");

export const SEED_PASSWORD = "DevPass123!";

export const organizers = users.filter((u) => u.role === "Organizer");
export const attendees = users.filter((u) => u.role === "Attendee");

export const alice = organizers[0];
export const bob = organizers[1];
export const dave = attendees[0];
