class Pidgeon < Formula
  desc "Healthcare message testing and validation CLI"
  homepage "https://pidgeon.health"
  version "0.1.0"

  if OS.mac?
    if Hardware::CPU.arm?
      url "https://github.com/PidgeonHealth/pidgeon/releases/download/v#{version}/pidgeon-osx-arm64.tar.gz"
      sha256 "placeholder-sha256-for-macos-arm64"
    else
      url "https://github.com/PidgeonHealth/pidgeon/releases/download/v#{version}/pidgeon-osx-x64.tar.gz"
      sha256 "placeholder-sha256-for-macos-x64"
    end
  elsif OS.linux?
    if Hardware::CPU.arm?
      url "https://github.com/PidgeonHealth/pidgeon/releases/download/v#{version}/pidgeon-linux-arm64.tar.gz"
      sha256 "placeholder-sha256-for-linux-arm64"
    else
      url "https://github.com/PidgeonHealth/pidgeon/releases/download/v#{version}/pidgeon-linux-x64.tar.gz"
      sha256 "placeholder-sha256-for-linux-x64"
    end
  end

  def install
    bin.install "pidgeon"

    # Install shell completions if they exist
    if File.exist?("completions/bash/pidgeon")
      bash_completion.install "completions/bash/pidgeon"
    end
    if File.exist?("completions/zsh/_pidgeon")
      zsh_completion.install "completions/zsh/_pidgeon"
    end
    if File.exist?("completions/fish/pidgeon.fish")
      fish_completion.install "completions/fish/pidgeon.fish"
    end
  end

  test do
    system "#{bin}/pidgeon", "--version"
    system "#{bin}/pidgeon", "--help"
  end
end