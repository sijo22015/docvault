-- Add MobileNumber and WhatsAppNumber columns to AspNetUsers
ALTER TABLE "AspNetUsers" ADD COLUMN IF NOT EXISTS "MobileNumber"   TEXT;
ALTER TABLE "AspNetUsers" ADD COLUMN IF NOT EXISTS "WhatsAppNumber" TEXT;
