/* Copyright © 2013-2015, Elián Hanisch <lambdae2@gmail.com>
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
        public GUIStyle tinyButton;
        public GUIStyle squareButton;
        public GUIStyle mainWindow;
        public GUIStyle mainWindowMinimized;
        public GUIStyle readoutName;

        public Style ()
        {
            setupStyle ();
        }

        void setupStyle ()
        {
            /* need a blank texture for the hover effect or it won't work */
            blankTexture = new Texture2D (1, 1, TextureFormat.Alpha8, false);
            blankTexture.SetPixel (0, 0, Color.clear);
            blankTexture.Apply ();

            GUI.skin.label.padding = new RectOffset ();
            GUI.skin.label.wordWrap = false;
            GUI.skin.toggle.padding = new RectOffset (15, 0, 0, 0);
            GUI.skin.toggle.overflow = new RectOffset (0, 0, -1, 0);

            readoutName = new GUIStyle (GUI.skin.label);
            readoutName.padding = new RectOffset ();
            readoutName.wordWrap = false;
            readoutName.fixedWidth = readout_label_width; 

            mainWindow = new GUIStyle (GUI.skin.window);
            mainWindow.alignment = TextAnchor.UpperLeft;
            mainWindow.fixedWidth = main_window_width;

            mainWindowMinimized = new GUIStyle (mainWindow);
            mainWindowMinimized.clipping = TextClipping.Overflow;

            centerText = new GUIStyle (GUI.skin.label);
            centerText.alignment = TextAnchor.MiddleCenter;
            centerText.wordWrap = true;

            smallButton = new GUIStyle (GUI.skin.button);
            smallButton.clipping = TextClipping.Overflow;
            smallButton.fixedHeight = GUI.skin.label.lineHeight;

            squareButton = new GUIStyle (smallButton);
            squareButton.fixedWidth = squareButton.fixedHeight;

            tinyButton = new GUIStyle (GUI.skin.button);
            tinyButton.clipping = TextClipping.Overflow;
            tinyButton.padding = new RectOffset (0, 2, 0, 3);
            tinyButton.margin = new RectOffset ();

            mainButton = new GUIStyle (smallButton);

            activeButton = new GUIStyle(mainButton);
            activeButton.normal = mainButton.onNormal;

            float w1, w2;
            GUI.skin.label.CalcMinMaxWidth(new GUIContent ("Size"), out w1, out w2);
            sizeLabel = new GUIStyle (GUI.skin.label);
            sizeLabel.fixedWidth = w2;
            sizeLabel.normal.textColor = GUI.skin.box.normal.textColor;

            resourceTableName = new GUIStyle (GUI.skin.label);
            resourceTableName.normal.textColor = GUI.skin.box.normal.textColor;
            resourceTableName.padding = GUI.skin.toggle.padding;

            clickLabel = new GUIStyle (GUI.skin.button);
            clickLabel.alignment = TextAnchor.LowerLeft;
            clickLabel.padding = new RectOffset ();
            clickLabel.normal = GUI.skin.label.normal;
            clickLabel.hover.background = blankTexture;
            clickLabel.hover.textColor = Color.yellow;
            clickLabel.active = clickLabel.hover;
            clickLabel.fixedHeight = GUI.skin.label.lineHeight;
            clickLabel.clipping = TextClipping.Overflow;

            clickLabelCenter = new GUIStyle (clickLabel);
            clickLabelCenter.alignment = TextAnchor.MiddleCenter;

            clickLabelGray = new GUIStyle(clickLabel);
            clickLabelGray.normal.textColor = GUI.skin.box.normal.textColor;

            resourceLabel = new GUIStyle(GUI.skin.label);
            resourceLabel.padding = GUI.skin.toggle.padding;

            listButton = new GUIStyle(clickLabel);
        }

    }
}

