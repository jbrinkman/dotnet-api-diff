class Dotnetapidiff < Formula
  desc "Command-line tool for comparing .NET assemblies and detecting API differences"
  homepage "https://github.com/jbrinkman/dotnet-api-diff"
  version "$version$"
  license "MIT"

  if OS.mac? && Hardware::CPU.arm?
    url "https://github.com/jbrinkman/dotnet-api-diff/releases/download/v$version$/dotnet-api-diff-osx-arm64.tar.gz"
    sha256 "$sha256_osx_arm64$"
  elsif OS.mac? && Hardware::CPU.intel?
    url "https://github.com/jbrinkman/dotnet-api-diff/releases/download/v$version$/dotnet-api-diff-osx-x64.tar.gz"
    sha256 "$sha256_osx_x64$"
  elsif OS.linux? && Hardware::CPU.arm64?
    url "https://github.com/jbrinkman/dotnet-api-diff/releases/download/v$version$/dotnet-api-diff-linux-arm64.tar.gz"
    sha256 "$sha256_linux_arm64$"
  elsif OS.linux? && Hardware::CPU.intel?
    url "https://github.com/jbrinkman/dotnet-api-diff/releases/download/v$version$/dotnet-api-diff-linux-x64.tar.gz"
    sha256 "$sha256_linux_x64$"
  end

  def install
    bin.install "DotNetApiDiff" => "dotnetapidiff"
  end

  test do
    system "#{bin}/dotnetapidiff", "--version"
  end
end
