NAME      = RCSBuildAid
MANAGED   = $(KSPDIR)/KSP_Data/Managed
GAMEDATA  = $(KSPDIR)/GameData
PLUGINDIR = $(GAMEDATA)/$(NAME)
PLUGIN    = bin/$(NAME).dll
SOURCES   = $(wildcard Plugin/*.cs Plugin/GUI/*.cs)

TOOLBAR   = bin/RCSBuildAidToolbar.dll
TOOLBAR_SRC = $(wildcard RCSBuildAidToolbar/*.cs)

VERSION = $(shell git describe --tags)
ZIPNAME = $(NAME)_$(VERSION).zip

GMCS = gmcs
CFLAGS = -optimize

all: plugin toolbar doc

plugin: $(PLUGIN)

toolbar: $(TOOLBAR)

$(PLUGIN): $(SOURCES) | check
	$(GMCS) $(CFLAGS) -t:library -lib:$(MANAGED) \
		-r:Assembly-CSharp,UnityEngine \
		-out:$@ $(SOURCES)

$(TOOLBAR): $(PLUGIN) $(TOOLBAR_SRC)
	$(GMCS) $(CFLAGS) -t:library -lib:$(MANAGED),$(GAMEDATA)/000_Toolbar \
		-r:Assembly-CSharp,UnityEngine,Toolbar,$(PLUGIN) \
		-out:$@ $(TOOLBAR_SRC)

doc: RCSBuildAid.version README.asciidoc CHANGELOG.asciidoc

clean:
	rm -rf $(PLUGIN) $(TOOLBAR)

define install_at
	mkdir -p $(1)/Plugins
    cp $(PLUGIN) $(1)/Plugins
    cp $(TOOLBAR) $(1)/Plugins
    mkdir -p $(1)/Textures
    cp Textures/*.png $(1)/Textures
    cp RCSBuildAid.version $(1)
    cp README.asciidoc $(1)
    cp CHANGELOG.asciidoc $(1)
endef

define install_src_at
	mkdir -p $(1)/Sources/GUI
	mkdir -p $(1)/Sources/RCSBuildAidToolbar
	cp Plugin/*.cs $(1)/Sources
	cp Plugin/GUI/*.cs $(1)/Sources/GUI
	cp RCSBuildAidToolbar/*.cs $(1)/Sources/RCSBuildAidToolbar
endef

install: all
	$(call install_at,$(PLUGINDIR))

package: all
	rm -rf Package/$(NAME)
	$(call install_at,Package/$(NAME))
	$(call install_src_at,Package/$(NAME))
	rm -f Package/$(ZIPNAME)
	cd Package && zip -r $(ZIPNAME) $(NAME)
	
uninstall: | check
	rm -rf $(PLUGINDIR)

check:
ifndef KSPDIR
	$(error KSPDIR envar not set)
endif

info:
	@echo "VERSION    $(VERSION)"
	@echo "KSP PATH   $(KSPDIR)"
	@echo "GAMEDATA   $(GAMEDATA)"
	@echo "PLUGIN DIR $(PLUGINDIR)"

version:
	@echo "$(VERSION)"

.PHONY: all plugin clean install uninstall check version doc package
