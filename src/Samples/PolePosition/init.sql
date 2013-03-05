CREATE INDEX BahrainPilot_Name      ON Starcounter.Poleposition.Circuits.Bahrain.Pilot (Name ASC);
CREATE INDEX BahrainPilot_LicenseId ON Starcounter.Poleposition.Circuits.Bahrain.Pilot (LicenseId ASC);
CREATE INDEX Barcelona2_Field2      ON Starcounter.Poleposition.Circuits.Barcelona.Barcelona2 (Field2 ASC);

CREATE INDEX InheritIndexHack_00    ON Starcounter.Poleposition.Circuits.Barcelona.Barcelona4 (Field2 ASC);
CREATE INDEX ExtentScanHack_00      ON Starcounter.Poleposition.Circuits.Imola.Pilot (LicenseId ASC);
CREATE INDEX ExtentScanHack_01      ON Starcounter.Poleposition.Circuits.Melbourne.Pilot (LicenseId ASC);
CREATE INDEX ExtentScanHack_02      ON Starcounter.Poleposition.Circuits.Sepang.Tree (Depth ASC);