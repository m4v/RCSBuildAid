NAME      =  RCSBuildAid
MANAGED   =  $(KSPDIR)/KSP_Data/Managed
GAMEDATA  =  $(KSPDIR)/GameData
PLUGINDIR =  $(GAMEDATA)/$(NAME)
BUILD     ?= $(PWD)/bin
PLUGIN    =  $(BUILD)/$(NAME).dll
SOURCES   =  $(wildcard Plugin/*.cs Plugin/GUI/*.cs)

TOOLBAR     =  $(BUILD)/RCSBuildAidToolbar.dll
TOOLBAR_SRC =  $(wildcard RCSBuildAidToolbar/*.cs)
TOOLBAR_LIB ?= $(GAMEDATA)/000_Toolbar

VERSION = $(shell git describe --tags --always)
ZIPNAME = $(NAME)_$(VERSION).zip

# "export GMCS=gmcs" for use old compiler
GMCS   ?= mcs -sdk:2
CFLAGS =  -optimize

# "export DEBUG=1" for enable debug build
ifeq ($(DEBUG), 1)
	CFLAGS = -debug -define:DEBUG
endif

all: plugin toolbar

info:
	@echo "VERSION    $(VERSION)"
	@echo "KSP PATH   $(KSPDIR)"
	@echo "GAMEDATA   $(GAMEDATA)"
	@echo "PLUGIN DIR $(PLUGINDIR)"
	@echo "BUILD PATH $(BUILD)"
	@echo "GMCS       $(GMCS)"
	@echo "CFLAGS     $(CFLAGS)"

plugin: $(PLUGIN)

toolbar: $(TOOLBAR)

$(PLUGIN): $(SOURCES) | check
	@echo "\n== Compiling $(NAME)"
	mkdir -p "$(BUILD)"
	$(GMCS) $(CFLAGS) -t:library -lib:"$(MANAGED)" \
		-r:Assembly-CSharp,UnityEngine \
		-out:$@ $(SOURCES)

$(TOOLBAR): $(PLUGIN) $(TOOLBAR_SRC) | check
	@echo "\n== Compiling toolbar support"
	mkdir -p "$(BUILD)"
	$(GMCS) $(CFLAGS) -t:library -lib:"$(MANAGED),$(TOOLBAR_LIB)" \
		-r:Assembly-CSharp,UnityEngine,Toolbar,$(PLUGIN) \
		-out:$@ $(TOOLBAR_SRC)

clean:
	rm -rfv "$(BUILD)"/*

define install_plugin_at
	@echo "\n== Installing $(NAME) at $(1)"
	mkdir -p "$(1)/Plugins"
	mkdir -p "$(1)/Textures"
	cp $(PLUGIN) "$(1)/Plugins"
	cp Textures/iconAppLauncher.png "$(1)/Textures"
	cp RCSBuildAid.version "$(1)"
	cp README.asciidoc "$(1)"
	cp CHANGELOG.asciidoc "$(1)"
	cp LICENSE "$(1)"
endef

define install_toolbar_at
	@echo "\n== Installing toolbar support at $(1)"
	mkdir -p "$(1)/Plugins"
	mkdir -p "$(1)/Textures"
	cp $(TOOLBAR) "$(1)/Plugins"
	cp Textures/iconToolbar.png "$(1)/Textures"
	cp Textures/iconToolbar_active.png "$(1)/Textures"
endef

define install_src_at
	@echo "\n== Copying source code at $(1)"
	mkdir -p "$(1)/Sources/GUI"
	mkdir -p "$(1)/Sources/RCSBuildAidToolbar"
	cp Plugin/*.cs "$(1)/Sources"
	cp Plugin/GUI/*.cs "$(1)/Sources/GUI"
	cp RCSBuildAidToolbar/*.cs "$(1)/Sources/RCSBuildAidToolbar"
endef

install: all install_plugin install_toolbar

install_plugin: plugin
	$(call install_plugin_at,$(PLUGINDIR))
ifeq ($(DEBUG),1)
	cp $(PLUGIN).mdb "$(PLUGINDIR)/Plugins"
endif

install_toolbar: toolbar
	$(call install_toolbar_at,$(PLUGINDIR))
ifeq ($(DEBUG),1)
	cp $(TOOLBAR).mdb "$(PLUGINDIR)/Plugins"
endif

package: all
	rm -rf Package/$(NAME)
	$(call install_plugin_at,Package/$(NAME))
	$(call install_toolbar_at,Package/$(NAME))
	$(call install_src_at,Package/$(NAME))
	@echo "\n== Making zip"
	rm -f Package/$(ZIPNAME)
	cd Package && zip -r $(ZIPNAME) $(NAME)
	
uninstall: | check
	rm -rfv "$(PLUGINDIR)"

check:
ifndef KSPDIR
	$(error KSPDIR envar not set)
endif


.PHONY: all plugin clean install uninstall check version package
