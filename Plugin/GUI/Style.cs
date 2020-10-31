/* Copyright © 2013-2020, Elián Hanisch <lambdae2@gmail.com>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using UnityEngine;

namespace RCSBuildAid
{
    public class Style
    {
        Texture2D blankTexture;

        public const int main_window_width = 184;
        public const int main_window_height = 52;
        public const int main_window_minimized_height = 26;
        public const int cbody_list_width = 112;
        public const int readout_label_width = 80;

        public GUIStyle centerText;
        public GUIStyle smallButton;
        public GUIStyle mainButton;
        public GUIStyle activeButton;
        public GUIStyle sizeLabel;
        public GUIStyle clickLabel;
        public GUIStyle clickLabelCenter;
        public GUIStyle resourceTableName;
        public GUIStyle clickLabelGray;
        public GUIStyle resourceLabel;
        public GUIStyle listButton;
        public GUIStyle listButtonDisabled;
        public GUIStyle tinyButton;
        public GUIStyle squareButton;
        public GUIStyle mainWindow;
        public GUIStyle mainWindowMinimized;
        public GUIStyle readoutName;
        public GUISkin skin;

        public Style ()
        {
            setupStyle ();
        }

        void setupStyle ()
        {
            skin = ScriptableObject.CreateInstance<GUISkin>();
            skin.label = new GUIStyle(GUI.skin.label);
            skin.button = new GUIStyle(GUI.skin.button);
            skin.window = new GUIStyle(GUI.skin.window);
            skin.toggle = new GUIStyle(GUI.skin.toggle);
            skin.box = new GUIStyle(GUI.skin.box);
            skin.horizontalSlider = new GUIStyle(GUI.skin.horizontalSlider);
            skin.horizontalSliderThumb = new GUIStyle(GUI.skin.horizontalSliderThumb);

            skin.label.padding = new RectOffset ();
            skin.label.wordWrap = false;
            skin.toggle.padding = new RectOffset (15, 0, 0, 0);
            skin.toggle.overflow = new RectOffset (0, 0, -1, 0);

            /* need a blank texture for the hover effect or it won't work */
            blankTexture = new Texture2D (1, 1, TextureFormat.Alpha8, false);
            blankTexture.SetPixel (0, 0, Color.clear);
            blankTexture.Apply ();

            readoutName = new GUIStyle (skin.label);
            readoutName.padding = new RectOffset ();
            readoutName.wordWrap = false;
            readoutName.fixedWidth = readout_label_width; 

            mainWindow = new GUIStyle (skin.window);
            mainWindow.alignment = TextAnchor.UpperLeft;
            mainWindow.fixedWidth = main_window_width;

            mainWindowMinimized = new GUIStyle (mainWindow);
            mainWindowMinimized.clipping = TextClipping.Overflow;

            centerText = new GUIStyle (skin.label);
            centerText.alignment = TextAnchor.MiddleCenter;
            centerText.wordWrap = true;

            smallButton = new GUIStyle (skin.button);
            smallButton.clipping = TextClipping.Overflow;
            smallButton.fixedHeight = skin.label.lineHeight;

            squareButton = new GUIStyle (smallButton);
            squareButton.fixedWidth = squareButton.fixedHeight;

            tinyButton = new GUIStyle (skin.button);
            tinyButton.clipping = TextClipping.Overflow;
            tinyButton.padding = new RectOffset (0, 2, 0, 3);
            tinyButton.margin = new RectOffset ();

            mainButton = new GUIStyle (smallButton);

            activeButton = new GUIStyle(mainButton);
            activeButton.normal = mainButton.onNormal;

            skin.label.CalcMinMaxWidth(new GUIContent ("Size"), out _, out var w2);
            sizeLabel = new GUIStyle (skin.label);
            sizeLabel.fixedWidth = w2;
            sizeLabel.normal.textColor = skin.box.normal.textColor;

            resourceTableName = new GUIStyle (skin.label);
            resourceTableName.normal.textColor = skin.box.normal.textColor;
            resourceTableName.padding = skin.toggle.padding;

            clickLabel = new GUIStyle (skin.button);
            clickLabel.alignment = TextAnchor.LowerLeft;
            clickLabel.padding = new RectOffset ();
            clickLabel.normal = skin.label.normal;
            clickLabel.hover.background = blankTexture;
            clickLabel.hover.textColor = Color.yellow;
            clickLabel.active = clickLabel.hover;
            clickLabel.fixedHeight = skin.label.lineHeight;
            clickLabel.clipping = TextClipping.Overflow;

            clickLabelCenter = new GUIStyle (clickLabel);
            clickLabelCenter.alignment = TextAnchor.MiddleCenter;

            clickLabelGray = new GUIStyle(clickLabel);
            clickLabelGray.normal.textColor = skin.box.normal.textColor;

            resourceLabel = new GUIStyle(skin.label);
            resourceLabel.padding = skin.toggle.padding;

            listButton = new GUIStyle(clickLabel);
            listButtonDisabled = new GUIStyle(listButton);
            listButtonDisabled.normal.textColor = Color.gray;
        }

    }
}

