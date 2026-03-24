{
  description = "Hasura docs development dependencies";

  inputs = {
    nixpkgs.url = "github:nixos/nixpkgs?ref=nixos-unstable";
    flake-utils.url = "github:numtide/flake-utils";
  };

  outputs = { self, nixpkgs, flake-utils }:
    flake-utils.lib.eachDefaultSystem (localSystem:
      let
        pkgs = import nixpkgs
          {
            system = localSystem;
            overlays = [ ];
          };
        nodejs-corepack = pkgs.stdenv.mkDerivation {
          name = "nodejs-corepack";
          buildInputs = [
            pkgs.nodejs
          ];
          phases = "installPhase";
          installPhase = ''
            mkdir -p $out/bin
            corepack enable --install-directory $out/bin
          '';
        };

      in

      {
        packages.${localSystem}.default = self.packages.${localSystem}.nodejs;

        devShells = {
          default = pkgs.mkShell {
            nativeBuildInputs = [
              # Development
              pkgs.nodejs
              nodejs-corepack
            ];
          };
        };
      }
    );
}
