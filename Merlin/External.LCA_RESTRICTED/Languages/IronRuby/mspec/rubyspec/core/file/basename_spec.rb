require File.dirname(__FILE__) + '/../../spec_helper'

describe "File.basename" do
  before :each do
    @name = tmp("test.txt")
    touch @name
  end

  after :each do
    rm_r @name
  end

  it "return the basename of a path (basic cases)" do
    File.basename(@name).should == "test.txt"
    File.basename(File.join("/tmp")).should == "tmp"
    File.basename(File.join(*%w( g f d s a b))).should == "b"
    File.basename("/tmp", ".*").should == "tmp"
    File.basename("/tmp", ".c").should == "tmp"
    File.basename("/tmp.c", ".c").should == "tmp"
    File.basename("/tmp.c", ".*").should == "tmp"
    File.basename("/tmp.c", ".?").should == "tmp.c"
    File.basename("/tmp.cpp", ".*").should == "tmp"
    File.basename("/tmp.cpp", ".???").should == "tmp.cpp"
    File.basename("/tmp.o", ".c").should == "tmp.o"
    File.basename(File.join("/tmp/")).should == "tmp"
    File.basename("/").should == "/"
    File.basename("//").should == "/"
    File.basename("dir///base", ".*").should == "base"
    File.basename("dir///base", ".c").should == "base"
    File.basename("dir///base.c", ".c").should == "base"
    File.basename("dir///base.c", ".*").should == "base"
    File.basename("dir///base.o", ".c").should == "base.o"
    File.basename("dir///base///").should == "base"
    File.basename("dir//base/", ".*").should == "base"
    File.basename("dir//base/", ".c").should == "base"
    File.basename("dir//base.c/", ".c").should == "base"
    File.basename("dir//base.c/", ".*").should == "base"
  end

  it "return the last component of the filename" do
    File.basename('a').should == 'a'
    File.basename('/a').should == 'a'
    File.basename('/a/b').should == 'b'
    File.basename('/ab/ba/bag').should == 'bag'
    File.basename('/ab/ba/bag.txt').should == 'bag.txt'
    File.basename('/').should == '/'
    File.basename('/foo/bar/baz.rb', '.rb').should == 'baz'
  end
  
  it "return the last component of the filename with multi-part extension" do
    File.basename('baz.txt.rb', '.rb').should == 'baz.txt'
    File.basename('baz.txt.rb', '.txt.rb').should == 'baz'
    File.basename('baz.txt.rb', 'z.txt.rb').should == 'ba'
    File.basename('baz.txt.rb', '.txt').should == 'baz.txt.rb'
    File.basename('baz.txt.rb', '').should == 'baz.txt.rb'
  end
  
  it "return the last component of the filename when extension filter does not begin with ." do
    File.basename('baz.rb', 'b').should == 'baz.r'
    File.basename('baz.rb', 'rb').should == 'baz.'
    File.basename('baz.rb', 'z.rb').should == 'ba'
    File.basename('baz.rb', 'foo').should == 'baz.rb'
    File.basename('baz.rb', '').should == 'baz.rb'
    deviates_on :rbx do
      File.basename("bar.txt.exe", ".txt.*").should == "bar"
    end
  end

  it "handles *" do
    File.basename('baz.rb', '.*').should == 'baz'
    File.basename('baz.rb', '*').should == 'baz.rb'
    File.basename('baz.rb', '.rb*').should == 'baz.rb'
    File.basename('baz.rb', '.*rb').should == 'baz.rb'

    File.basename('baz.txt.rb', '.*').should == 'baz.txt'
    File.basename('baz.txt.rb', '*').should == 'baz.txt.rb'
    File.basename('baz.txt.rb', 't*').should == 'baz.tx'
  end
  
  it "return an string" do
    File.basename("foo").class.should == String
  end

  it "return the basename for unix format" do
    File.basename("/foo/bar").should == "bar"
    File.basename("/foo/bar.txt").should == "bar.txt"
    File.basename("bar.c").should == "bar.c"
    File.basename("/bar").should == "bar"
    File.basename("/bar/").should == "bar"
  end

  it "return the basename for edge cases" do
    File.basename("").should == ""
    File.basename(".").should == "."
    File.basename("..").should == ".."
    File.basename("foo/").should == "foo"
  end

  platform_is :windows do
    it "return the basename for edge cases (Windows)" do
      # Considered UNC paths on Windows
      File.basename("//foo/").should == "/"
      File.basename("//foo//").should == "/"
      File.basename("baz//foo").should =="foo"
      File.basename("//foo/bar/baz").should == "baz"
    end
  end
  
  platform_is_not :windows do
    it "return the basename for edge cases (non-Windows)" do
      File.basename("//foo/").should == "foo"
      File.basename("//foo//").should == "foo"
    end
  end
  
  it "return the basename for unix suffix" do
    File.basename("bar.c", ".c").should == "bar"
    File.basename("bar.txt", ".txt").should == "bar"
    File.basename("/bar.txt", ".txt").should == "bar"
    File.basename("/foo/bar.txt", ".txt").should == "bar"
    File.basename("bar.txt", ".exe").should == "bar.txt"
    File.basename("bar.txt.exe", ".exe").should == "bar.txt"
    File.basename("bar.txt.exe", ".txt").should == "bar.txt.exe"
    File.basename("bar.txt", ".*").should == "bar"
    File.basename("bar.txt.exe", ".*").should == "bar.txt"
    File.basename("bar.txt.exe", ".txt.exe").should == "bar"
    deviates_on :rbx do
      File.basename("bar.txt.exe", ".txt.*").should == "bar"
    end
  end

  it "raises a TypeError if the arguments are not String types" do
    lambda { File.basename(nil)          }.should raise_error(TypeError)
    lambda { File.basename(1)            }.should raise_error(TypeError)
    lambda { File.basename("bar.txt", 1) }.should raise_error(TypeError)
    lambda { File.basename(true)         }.should raise_error(TypeError)
  end

  ruby_version_is "1.9" do
    it "accepts an object that has a #to_path method" do
      File.basename(mock_to_path("foo.txt"))
    end
  end

  it "raises an ArgumentError if passed more than two arguments" do
    lambda { File.basename('bar.txt', '.txt', '.txt') }.should raise_error(ArgumentError)
  end

  # specific to MS Windows
  platform_is :windows do
    it "return the basename for windows" do
      File.basename("C:\\foo\\bar\\baz.txt").should == "baz.txt"
      File.basename("C:\\foo\\bar").should == "bar"
      File.basename("C:\\foo\\bar\\").should == "bar"
      File.basename("C:\\foo").should == "foo"
      File.basename("C:\\").should == "\\"
    end

    it "return basename windows unc" do
      File.basename("\\\\foo\\bar\\baz.txt").should == "baz.txt"
      File.basename("\\\\foo\\bar\\baz").should =="baz"
      File.basename("\\\\foo").should == "/"
      File.basename("\\\\foo\\bar").should == "/"
    end

    it "return basename windows forward slash" do
      File.basename("C:/").should == "/"
      File.basename("C:/foo").should == "foo"
      File.basename("C:/foo/bar").should == "bar"
      File.basename("C:/foo/bar/").should == "bar"
      File.basename("C:/foo/bar//").should == "bar"
    end

    it "return basename with windows suffix" do
      File.basename("c:\\bar.txt", ".txt").should == "bar"
      File.basename("c:\\foo\\bar.txt", ".txt").should == "bar"
      File.basename("c:\\bar.txt", ".exe").should == "bar.txt"
      File.basename("c:\\bar.txt.exe", ".exe").should == "bar.txt"
      File.basename("c:\\bar.txt.exe", ".txt").should == "bar.txt.exe"
      File.basename("c:\\bar.txt", ".*").should == "bar"
      File.basename("c:\\bar.txt.exe", ".*").should == "bar.txt"
    end
  end
end
